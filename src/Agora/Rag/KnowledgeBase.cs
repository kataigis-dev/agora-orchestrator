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

    public KnowledgeBase(
        IEmbedder embedder,
        IVectorStore store,
        IConflictJudge judge,
        IConflictResolver? resolver = null,
        int neighborK = 5,
        double scoreThreshold = 0.5)
    {
        _embedder = embedder;
        _store = store;
        _judge = judge;
        _resolver = resolver;
        _neighborK = neighborK;
        _scoreThreshold = scoreThreshold;
    }

    public async Task<WriteResult> WriteAsync(
        string text, string source = "agent", string agentId = "", CancellationToken cancellationToken = default)
    {
        var vector = await EmbedOne(text, cancellationToken);
        var neighbors = _store.Query(vector, _neighborK, _scoreThreshold);

        if (neighbors.Count == 0)
        {
            Add(text, vector, source);
            return new WriteResult(WriteOutcome.Added, text);
        }

        var assessment = await _judge.AssessAsync(text, neighbors, cancellationToken);
        switch (assessment.Verdict)
        {
            case ConflictVerdict.NoConflict:
                Add(text, vector, source);
                return new WriteResult(WriteOutcome.NoConflict, text);

            case ConflictVerdict.Resolved:
                var resolved = string.IsNullOrWhiteSpace(assessment.ResolvedText) ? text : assessment.ResolvedText!;
                await ReplaceAsync(assessment.Conflicting, resolved, source, cancellationToken);
                return new WriteResult(WriteOutcome.AutoResolved, resolved, assessment.Explanation);

            case ConflictVerdict.Unresolved:
                return await EscalateAsync(text, vector, source, agentId, neighbors, assessment, cancellationToken);

            default:
                Add(text, vector, source);
                return new WriteResult(WriteOutcome.NoConflict, text);
        }
    }

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
            if (ids.Count > 0) _store.Delete(ids);
        }
        Add(text, vector ?? await EmbedOne(text, cancellationToken), source);
    }

    private void Add(string text, float[] vector, string source)
        => _store.Upsert(new[] { new Chunk(text, source) }, new[] { vector });

    private async Task<float[]> EmbedOne(string text, CancellationToken cancellationToken)
        => (await _embedder.EmbedAsync(new[] { text }, cancellationToken))[0];
}
