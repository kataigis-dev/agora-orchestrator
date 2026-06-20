namespace Agora.Rag;

/// <summary>Never reports a conflict — used when no chat provider is available to judge.</summary>
public sealed class NoOpConflictJudge : IConflictJudge
{
    public Task<ConflictAssessment> AssessAsync(
        string newEntry, IReadOnlyList<Chunk> existing, CancellationToken cancellationToken = default)
        => Task.FromResult(new ConflictAssessment(ConflictVerdict.NoConflict));
}
