namespace Agora.HumanInTheLoop;

/// <summary>Deterministic approval handler for tests: always approves or always denies; records requests.</summary>
public sealed class FakeApprovalHandler : IApprovalHandler
{
    private readonly bool _approve;

    public FakeApprovalHandler(bool approve = true) => _approve = approve;

    public List<ApprovalRequest> Requests { get; } = new();

    public Task<bool> RequestAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(_approve);
    }
}
