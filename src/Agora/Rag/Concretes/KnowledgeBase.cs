using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using System.Security.Cryptography;
using System.Text;
using Agora.HumanInTheLoop;

namespace Agora.Rag.Concretes;

public enum WriteOutcome
{
    /// <summary>Stored — the knowledge base had no related entries to conflict with.</summary>
    Added,

    /// <summary>Stored — related entries existed but the judge found no conflict.</summary>
    NoConflict,

    /// <summary>Stored after a human resolved the conflict (kept new, or merged — possibly accepting the
    /// judge's suggested reconciliation). The judge only proposes; it never applies a merge on its own.</summary>
    UserResolved,

    /// <summary>Nothing stored — human kept existing, or (defensively) no resolver was available.</summary>
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
    private readonly IKbMutationLog? _log;
    private readonly int _neighborK;
    private readonly double _scoreThreshold;

    private readonly double _conflictThreshold;
    private readonly Dictionary<string, ConflictAssessment> _assessmentCache = new();
    private const int MaxCacheEntries = 512;

    // A graph runs parallel branches against one KnowledgeBase. The query→judge→resolve→apply sequence
    // is not atomic — without this lock branch A could delete a neighbour while branch B is judging
    // against it — and the assessment cache is a plain Dictionary. This serialises the whole critical
    // section (and the human gate widens it to human-length waits, see issue 07).
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>Creates the knowledge base over a shared embedder/store, with the conflict judge,
    /// optional human resolver, and the neighbor/conflict similarity thresholds.</summary>
    public KnowledgeBase(
        IEmbedder embedder,
        IVectorStore store,
        IConflictJudge judge,
        IConflictResolver? resolver = null,
        int neighborK = 5,
        double scoreThreshold = 0.5,
        double conflictThreshold = 0.8,
        IKbMutationLog? mutationLog = null)
    {
        _embedder = embedder;
        _store = store;
        _judge = judge;
        _resolver = resolver;
        _log = mutationLog;
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
        // Embedding touches no shared state, so it stays outside the critical section.
        var vector = await EmbedOne(text, cancellationToken);

        // Serialise the query→judge→resolve→apply section so parallel branches can't interleave a
        // delete with another branch's judging, and so the assessment cache stays a safe single-writer.
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var neighbors = await _store.QueryAsync(vector, _neighborK, _scoreThreshold, cancellationToken);

            if (neighbors.Count == 0)
            {
                await AddAsync(text, vector, source, cancellationToken);
                await LogAddAsync(agentId, text, "added (no related entries)", "", cancellationToken);
                return new WriteResult(WriteOutcome.Added, text);
            }

            // Prefilter: only spend an LLM judge call when a neighbor is close enough to plausibly
            // conflict; loosely-related entries (below conflictThreshold) are added without judging.
            if (neighbors[0].Score < _conflictThreshold)
            {
                await AddAsync(text, vector, source, cancellationToken);
                await LogAddAsync(agentId, text, "added (below conflict threshold)", "", cancellationToken);
                return new WriteResult(WriteOutcome.NoConflict, text, "below conflict threshold");
            }

            var assessment = await AssessCachedAsync(text, neighbors, cancellationToken);
            switch (assessment.Verdict)
            {
                case ConflictVerdict.NoConflict:
                    await AddAsync(text, vector, source, cancellationToken);
                    await LogAddAsync(agentId, text, "added (no conflict)", assessment.Explanation, cancellationToken);
                    return new WriteResult(WriteOutcome.NoConflict, text);

                // The judge only detects and *proposes*. Both Resolved and Unresolved are conflicts that
                // a human must decide — no entry is ever deleted or replaced autonomously. A Resolved
                // verdict rides its reconciliation along as the suggested merge.
                case ConflictVerdict.Resolved:
                case ConflictVerdict.Unresolved:
                    return await EscalateAsync(text, vector, source, agentId, assessment, cancellationToken);

                default:
                    await AddAsync(text, vector, source, cancellationToken);
                    await LogAddAsync(agentId, text, "added", "", cancellationToken);
                    return new WriteResult(WriteOutcome.NoConflict, text);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>Asks the human resolver to settle a conflict and applies the decision (keep-new/merge
    /// replace the conflicting entries; keep-existing rejects the write). The human sees only the entries
    /// the judge explicitly named (issue 05) and, for a Resolved verdict, the judge's reconciliation as
    /// the suggested merge — the judge proposes, the human decides.</summary>
    private async Task<WriteResult> EscalateAsync(
        string text, float[] vector, string source, string agentId,
        ConflictAssessment assessment, CancellationToken cancellationToken)
    {
        var conflicting = assessment.Conflicting ?? Array.Empty<Chunk>();
        if (_resolver is null)
            return new WriteResult(WriteOutcome.Rejected, text, "conflict requires a human resolver; none available");

        var decision = await _resolver.ResolveAsync(new ConflictResolutionRequest
        {
            AgentId = agentId,
            NewEntry = text,
            ExistingEntries = conflicting.Select(n => n.Text).ToList(),
            Explanation = assessment.Explanation,
            SuggestedMerge = assessment.ResolvedText ?? "",
        }, cancellationToken);

        switch (decision.Resolution)
        {
            case ConflictResolution.KeepNew:
                await ReplaceAsync(conflicting, text, source, cancellationToken, vector);
                await LogReplaceAsync(agentId, conflicting, text, "kept new", assessment.Explanation, cancellationToken);
                return new WriteResult(WriteOutcome.UserResolved, text, "kept new");

            case ConflictResolution.Merge:
                // Default the merge to the judge's suggestion when the human supplied none.
                var merged = FirstNonBlank(decision.MergedText, assessment.ResolvedText, text);
                await ReplaceAsync(conflicting, merged, source, cancellationToken);
                await LogReplaceAsync(agentId, conflicting, merged, "merged", assessment.Explanation, cancellationToken);
                return new WriteResult(WriteOutcome.UserResolved, merged, "merged");

            case ConflictResolution.KeepExisting:
            default:
                // Nothing stored or removed → no mutation to record.
                return new WriteResult(WriteOutcome.Rejected, text, "kept existing");
        }
    }

    /// <summary>Returns the first non-blank value (used to default a merge to the judge's suggestion).</summary>
    private static string FirstNonBlank(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";

    /// <summary>Records a plain add in the mutation log (no-op when no log is configured).</summary>
    private Task LogAddAsync(string agentId, string newText, string decision, string explanation, CancellationToken ct)
        => _log is null ? Task.CompletedTask : _log.AppendAsync(new KbMutation
        {
            Kind = KbMutationKind.Add, AgentId = agentId, NewText = newText,
            Decision = decision, JudgeExplanation = explanation,
        }, ct);

    /// <summary>Records a human-confirmed replacement (superseded entries → reconciled text) in the log.</summary>
    private Task LogReplaceAsync(
        string agentId, IReadOnlyList<Chunk> superseded, string newText, string decision, string explanation,
        CancellationToken ct)
        => _log is null ? Task.CompletedTask : _log.AppendAsync(new KbMutation
        {
            Kind = KbMutationKind.Replace, AgentId = agentId,
            OldText = superseded.Select(c => c.Text).ToList(), NewText = newText,
            Decision = decision, JudgeExplanation = explanation,
        }, ct);

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
