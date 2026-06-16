using System.Collections.Concurrent;
using Agora.HumanInTheLoop;

namespace Agora.Runs;

/// <summary>
/// Registry of tool-approval requests parked on a <see cref="TaskCompletionSource{Boolean}"/>.
/// The run background task awaits <see cref="WaitAsync"/>; an HTTP request resolves it via <see cref="Resolve"/>.
/// </summary>
public sealed class ApprovalGate
{
    private sealed record Entry(PendingApproval Approval, TaskCompletionSource<bool> Tcs, string RunId);

    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    public Task<bool> WaitAsync(string runId, ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var approval = new PendingApproval
        {
            Id = id,
            AgentId = request.AgentId,
            FunctionName = request.FunctionName,
            Arguments = request.Arguments,
        };
        _entries[id] = new Entry(approval, tcs, runId);
        if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(() =>
            {
                if (_entries.TryRemove(id, out var e))
                    e.Tcs.TrySetCanceled(cancellationToken);
            });
        return tcs.Task;
    }

    public IReadOnlyList<PendingApproval> Pending(string runId)
        => _entries.Values.Where(e => e.RunId == runId).Select(e => e.Approval).ToList();

    public bool Resolve(string approvalId, bool approved)
        => _entries.TryRemove(approvalId, out var e) && e.Tcs.TrySetResult(approved);
}
