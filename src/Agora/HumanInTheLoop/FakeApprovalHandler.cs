namespace Agora.HumanInTheLoop;

/// <summary>Deterministic approval handler for tests: always approves or always denies; records requests.</summary>
public sealed class FakeApprovalHandler : IApprovalHandler
{
    private readonly bool _approve;

    /// <summary>Creates the handler that always returns <paramref name="approve"/>.</summary>
    public FakeApprovalHandler(bool approve = true) => _approve = approve;

    /// <summary>Records the approval requests received, for test assertions.</summary>
    public List<ApprovalRequest> Requests { get; } = new();

    /// <inheritdoc />
    public Task<bool> RequestAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(_approve);
    }
}
