using Agora.Runs;

namespace Agora.Api;

/// <summary>Drains the run queue, executing each run on a background thread.</summary>
public sealed class RunExecutor : BackgroundService
{
    private readonly RunQueue _queue;
    private readonly IRunStore _store;
    private readonly ApprovalGate _gate;
    private readonly AgoraRuntimeFactory _runtimes;

    /// <summary>Creates the background executor with the queue, run store, approval gate, and runtime factory.</summary>
    public RunExecutor(RunQueue queue, IRunStore store, ApprovalGate gate, AgoraRuntimeFactory runtimes)
    {
        _queue = queue;
        _store = store;
        _gate = gate;
        _runtimes = runtimes;
    }

    /// <summary>Continuously dequeues run ids and executes them until the service stops.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var runId in _queue.ReadAllAsync(stoppingToken))
            await RunOne(runId);
    }

    /// <summary>Executes one run (agent or graph) and records its completion or failure.</summary>
    private async Task RunOne(string runId)
    {
        var record = _store.Get(runId);
        if (record is null)
            return;
        try
        {
            var runtime = _runtimes.Build(new PendingApprovalHandler(runId, _gate, _store));
            var output = record.Mode == "graph"
                ? (await runtime.RunAsync(record.Input)).Output
                : (await runtime.RunAgentAsync(record.AgentId!, record.Input)).Output;
            _store.Update(runId, r => { r.Status = RunStatus.Completed; r.Output = output; });
        }
        catch (Exception ex)
        {
            _store.Update(runId, r => { r.Status = RunStatus.Failed; r.Error = ex.Message; });
        }
    }
}
