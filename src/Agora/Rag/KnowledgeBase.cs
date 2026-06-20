using System.Security.Cryptography;
using System.Text;
using Agora.HumanInTheLoop;

namespace Agora.Rag;

public enum WriteOutcome
{
    /// <summary>Stored — the knowledge base had no related entries to conflict with.</summary>
    Added,

    /// <summary>Stored — related entries existed but the judge found no conflict.</summary>
    NoConflict,

    /// <summary>Stored a reconciled entry — the judge resolved the conflict on its own.</summary>
    AutoResolved,

    /// <summary>Stored after a human resolved the conflict (kept new or merged).</summary>
    UserResolved,

    /// <summary>Nothing stored — conflict left unresolved (human kept existing, or no resolver available).</summary>
    Rejected,
}

/// <summary>Result of a knowledge-base write: the outcome, the text actually stored, and a detail note.</summary>
public sealed record WriteResult(WriteOutcome Outcome, string StoredText, string Detail = "");

/// <summary>
/// Agent-facing write path into the shared knowledge base. Embeds the new entry,
/// finds related existing entries, asks an <see cref="IConflictJudge"/> whether it
/// conflicts, and — when the judge cannot resolve it — escalates to an
/// <see cref="IConflictResolver"/> (human). Reads still go through <see cref="RagPipeline"/>.
/// </summary>
public sealed class KnowledgeBase
{
    private readonly IEmbedder _embedder;
    private readonly IVectorStore _store;
    private readonly IConflictJudge _judge;
    private readonly IConflictResolver? _resolver;
    private readonly int _neighborK;
    private readonly double _scoreThreshold;

    private readonly double _conflictThreshold;
    private readonly Dictionary<string, ConflictAssessment> _assessmentCache = new();
    private const int MaxCacheEntries = 512;

    /// <summary>Creates the knowledge base over a shared embedder/store, with the conflict judge,
    /// optional human resolver, and the neighbor/conflict similarity thresholds.</summary>
    public KnowledgeBase(
        IEmbedder embedder,
        IVectorStore store,
        IConflictJudge judge,
        IConflictResolver? resolver = null,
        int neighborK = 5,
        double scoreThreshold = 0.5,
        double conflictThreshold = 0.8)
    {
        _embedder = embedder;
        _store = store;
        _judge = judge;
        _resolver = resolver;
        _neighborK = neighborK;
        _scoreThreshold = scoreThreshold;
        _conflictThreshold = conflictThreshold;
    }

    /// <summary>Writes an entry: embeds it, finds related neighbors, and adds it directly when there
    /// are none or none close enough; otherwise judges the conflict and either adds, replaces with a
    /// reconciled entry, or escalates to the human resolver.</summary>
    public async Task<WriteResult> WriteAsync(
        string text, string source = "agent", string agentId = "", CancellationToken cancellationToken = default)
    {
        var vector = await EmbedOne(text, cancellationToken);
        var neighbors = await _store.QueryAsync(vector, _neighborK, _scoreThreshold, cancellationToken);

        if (neighbors.Count == 0)
        {
            await AddAsync(text, vector, source, cancellationToken);
            return new WriteResult(WriteOutcome.Added, text);
        }

        // Prefilter: only spend an LLM judge call when a neighbor is close enough to plausibly
        // conflict; loosely-related entries (below conflictThreshold) are added without judging.
        if (neighbors[0].Score < _conflictThreshold)
        {
            await AddAsync(text, vector, source, cancellationToken);
            return new WriteResult(WriteOutcome.NoConflict, text, "below conflict threshold");
        }

        var assessment = await AssessCachedAsync(text, neighbors, cancellationToken);
        switch (assessment.Verdict)
        {
            case ConflictVerdict.NoConflict:
                await AddAsync(text, vector, source, cancellationToken);
                return new WriteResult(WriteOutcome.NoConflict, text);

            case ConflictVerdict.Resolved:
                var resolved = string.IsNullOrWhiteSpace(assessment.ResolvedText) ? text : assessment.ResolvedText!;
                await ReplaceAsync(assessment.Conflicting, resolved, source, cancellationToken);
                return new WriteResult(WriteOutcome.AutoResolved, resolved, assessment.Explanation);

            case ConflictVerdict.Unresolved:
                return await EscalateAsync(text, vector, source, agentId, neighbors, assessment, cancellationToken);

            default:
                await AddAsync(text, vector, source, cancellationToken);
                return new WriteResult(WriteOutcome.NoConflict, text);
        }
    }

    /// <summary>Asks the human resolver to settle an unresolved conflict and applies the decision
    /// (keep-new/merge replace the conflicting entries; keep-existing rejects the write).</summary>
    private async Task<WriteResult> EscalateAsync(
        string text, float[] vector, string source, string agentId,
        IReadOnlyList<Chunk> neighbors, ConflictAssessment assessment, CancellationToken cancellationToken)
    {
        if (_resolver is null)
            return new WriteResult(WriteOutcome.Rejected, text, "unresolved conflict and no resolver available");

        var decision = await _resolver.ResolveAsync(new ConflictResolutionRequest
        {
            AgentId = agentId,
            NewEntry = text,
            ExistingEntries = neighbors.Select(n => n.Text).ToList(),
            Explanation = assessment.Explanation,
        }, cancellationToken);

        switch (decision.Resolution)
        {
            case ConflictResolution.KeepNew:
                await ReplaceAsync(assessment.Conflicting, text, source, cancellationToken, vector);
                return new WriteResult(WriteOutcome.UserResolved, text, "kept new");

            case ConflictResolution.Merge:
                var merged = string.IsNullOrWhiteSpace(decision.MergedText) ? text : decision.MergedText!;
                await ReplaceAsync(assessment.Conflicting, merged, source, cancellationToken);
                return new WriteResult(WriteOutcome.UserResolved, merged, "merged");

            case ConflictResolution.KeepExisting:
            default:
                return new WriteResult(WriteOutcome.Rejected, text, "kept existing");
        }
    }

    /// <summary>Removes the entries superseded by a resolution, then stores the reconciled entry.</summary>
    private async Task ReplaceAsync(
        IReadOnlyList<Chunk>? superseded, string text, string source, CancellationToken cancellationToken,
        float[]? vector = null)
    {
        if (superseded is { Count: > 0 })
        {
            var ids = superseded.Select(c => c.Id).Where(id => !string.IsNullOrEmpty(id)).ToList();
            if (ids.Count > 0) await _store.DeleteAsync(ids, cancellationToken);
        }
        await AddAsync(text, vector ?? await EmbedOne(text, cancellationToken), source, cancellationToken);
    }

    /// <summary>Inserts a new entry (with its vector) into the store.</summary>
    private Task AddAsync(string text, float[] vector, string source, CancellationToken cancellationToken)
        => _store.UpsertAsync(new[] { new Chunk(text, source) }, new[] { vector }, cancellationToken);

    /// <summary>Embeds a single string and returns its vector.</summary>
    private async Task<float[]> EmbedOne(string text, CancellationToken cancellationToken)
        => (await _embedder.EmbedAsync(new[] { text }, cancellationToken))[0];

    /// <summary>Judges a write, caching by (new text + neighbor texts) so identical writes don't
    /// re-invoke the LLM. Safe: once a conflict is resolved the superseded neighbors are deleted,
    /// which changes the neighbor set (and thus the key) for later writes.</summary>
    private async Task<ConflictAssessment> AssessCachedAsync(
        string text, IReadOnlyList<Chunk> neighbors, CancellationToken cancellationToken)
    {
        var key = CacheKey(text, neighbors);
        if (_assessmentCache.TryGetValue(key, out var cached))
            return cached;
        var assessment = await _judge.AssessAsync(text, neighbors, cancellationToken);
        if (_assessmentCache.Count >= MaxCacheEntries)
            _assessmentCache.Clear();
        _assessmentCache[key] = assessment;
        return assessment;
    }

    /// <summary>Builds a stable hash key from the new text and its neighbor texts for the judge cache.</summary>
    private static string CacheKey(string text, IReadOnlyList<Chunk> neighbors)
    {
        var joined = text + "\u0001" + string.Join("\u0001", neighbors.Select(n => n.Text));
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(joined)));
    }
}
