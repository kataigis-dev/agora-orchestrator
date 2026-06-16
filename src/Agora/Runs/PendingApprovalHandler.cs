using Agora.HumanInTheLoop;

namespace Agora.Runs;

/// <summary>Adapts <see cref="ApprovalGate"/> to <see cref="IApprovalHandler"/> for one run, tracking status.</summary>
public sealed class PendingApprovalHandler : IApprovalHandler
{
    private readonly string _runId;
    private readonly ApprovalGate _gate;
    private readonly IRunStore _store;

    public PendingApprovalHandler(string runId, ApprovalGate gate, IRunStore store)
    {
        _runId = runId;
        _gate = gate;
        _store = store;
    }

    public async Task<bool> RequestAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        _store.Update(_runId, r => r.Status = RunStatus.AwaitingApproval);
        try
        {
            return await _gate.WaitAsync(_runId, request, cancellationToken);
        }
        finally
        {
            _store.Update(_runId, r => { if (r.Status == RunStatus.AwaitingApproval) r.Status = RunStatus.Running; });
        }
    }
}
