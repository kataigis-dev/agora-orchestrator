namespace Agora.Rag;

public enum ConflictVerdict
{
    /// <summary>The new entry adds to the knowledge base without contradicting anything.</summary>
    NoConflict,

    /// <summary>A conflict existed but the judge reconciled it into a single statement.</summary>
    Resolved,

    /// <summary>A conflict the judge cannot reconcile on its own — needs human judgement.</summary>
    Unresolved,
}

/// <summary>Outcome of comparing a new knowledge entry against existing related entries.</summary>
public sealed record ConflictAssessment(
    ConflictVerdict Verdict,
    string? ResolvedText = null,
    string Explanation = "",
    IReadOnlyList<Chunk>? Conflicting = null);

/// <summary>Decides whether writing a new entry to the knowledge base conflicts with what is already there.</summary>
public interface IConflictJudge
{
    Task<ConflictAssessment> AssessAsync(
        string newEntry, IReadOnlyList<Chunk> existing, CancellationToken cancellationToken = default);
}
