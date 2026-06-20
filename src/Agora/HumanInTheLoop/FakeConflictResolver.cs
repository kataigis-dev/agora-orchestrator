namespace Agora.HumanInTheLoop;

/// <summary>Deterministic conflict resolver for tests: returns a preset decision; records requests.</summary>
public sealed class FakeConflictResolver : IConflictResolver
{
    private readonly ConflictDecision _decision;

    /// <summary>Creates the resolver that always returns the given resolution (and optional merged text).</summary>
    public FakeConflictResolver(ConflictResolution resolution = ConflictResolution.KeepNew, string? mergedText = null)
        => _decision = new ConflictDecision { Resolution = resolution, MergedText = mergedText };

    /// <summary>Records the resolution requests received, for test assertions.</summary>
    public List<ConflictResolutionRequest> Requests { get; } = new();

    /// <inheritdoc />
    public Task<ConflictDecision> ResolveAsync(
        ConflictResolutionRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(_decision);
    }
}
