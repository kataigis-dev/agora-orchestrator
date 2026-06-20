namespace Agora.HumanInTheLoop;

/// <summary>Deterministic conflict resolver for tests: returns a preset decision; records requests.</summary>
public sealed class FakeConflictResolver : IConflictResolver
{
    private readonly ConflictDecision _decision;

    public FakeConflictResolver(ConflictResolution resolution = ConflictResolution.KeepNew, string? mergedText = null)
        => _decision = new ConflictDecision { Resolution = resolution, MergedText = mergedText };

    public List<ConflictResolutionRequest> Requests { get; } = new();

    public Task<ConflictDecision> ResolveAsync(
        ConflictResolutionRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(_decision);
    }
}
