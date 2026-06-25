namespace Agora.HumanInTheLoop;

public enum ConflictResolution
{
    /// <summary>Discard the new entry; keep what is already in the knowledge base.</summary>
    KeepExisting,

    /// <summary>Store the new entry as written.</summary>
    KeepNew,

    /// <summary>Store a human-supplied reconciliation of old and new.</summary>
    Merge,
}

/// <summary>A knowledge-base conflict escalated to a human because the agent could not resolve it.</summary>
public sealed record ConflictResolutionRequest
{
    /// <summary>Agent attempting the write.</summary>
    public required string AgentId { get; init; }

    /// <summary>The new entry the agent wants to store.</summary>
    public required string NewEntry { get; init; }

    /// <summary>The existing entries it conflicts with.</summary>
    public required IReadOnlyList<string> ExistingEntries { get; init; }

    /// <summary>The judge's explanation of the conflict.</summary>
    public string Explanation { get; init; } = "";

    /// <summary>The judge's proposed reconciliation, offered to the human as the default merge text.
    /// Empty when the judge proposed none (e.g. an unresolved conflict). The judge only suggests —
    /// the human decides whether to accept it.</summary>
    public string SuggestedMerge { get; init; } = "";
}

/// <summary>A human's decision on how to resolve a knowledge-base conflict.</summary>
public sealed record ConflictDecision
{
    /// <summary>The chosen resolution.</summary>
    public required ConflictResolution Resolution { get; init; }

    /// <summary>The reconciled text, required when <see cref="Resolution"/> is <see cref="ConflictResolution.Merge"/>.</summary>
    public string? MergedText { get; init; }
}

/// <summary>Asks a human how to resolve a knowledge-base conflict. Injected at the edges (CLI / API / test fake).</summary>
public interface IConflictResolver
{
    /// <summary>Resolves a conflict, returning the human's decision.</summary>
    Task<ConflictDecision> ResolveAsync(
        ConflictResolutionRequest request, CancellationToken cancellationToken = default);
}
