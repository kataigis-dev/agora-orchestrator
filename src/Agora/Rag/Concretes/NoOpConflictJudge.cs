using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
namespace Agora.Rag.Concretes;

/// <summary>Never reports a conflict — used when no chat provider is available to judge.</summary>
public sealed class NoOpConflictJudge : IConflictJudge
{
    /// <inheritdoc />
    public Task<ConflictAssessment> AssessAsync(
        string newEntry, IReadOnlyList<Chunk> existing, CancellationToken cancellationToken = default)
        => Task.FromResult(new ConflictAssessment(ConflictVerdict.NoConflict));
}
