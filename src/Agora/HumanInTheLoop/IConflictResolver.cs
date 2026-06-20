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
    public required string AgentId { get; init; }
    public required string NewEntry { get; init; }
    public required IReadOnlyList<string> ExistingEntries { get; init; }
    public string Explanation { get; init; } = "";
}

public sealed record ConflictDecision
{
    public required ConflictResolution Resolution { get; init; }

    /// <summary>The reconciled text, required when <see cref="Resolution"/> is <see cref="ConflictResolution.Merge"/>.</summary>
    public string? MergedText { get; init; }
}

/// <summary>Asks a human how to resolve a knowledge-base conflict. Injected at the edges (CLI / API / test fake).</summary>
public interface IConflictResolver
{
    Task<ConflictDecision> ResolveAsync(
        ConflictResolutionRequest request, CancellationToken cancellationToken = default);
}
