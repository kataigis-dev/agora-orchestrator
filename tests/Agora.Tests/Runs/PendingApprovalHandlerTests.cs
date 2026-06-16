using Agora.HumanInTheLoop;
using Agora.Runs;
using Xunit;

namespace Agora.Tests.Runs;

public class PendingApprovalHandlerTests
{
    [Fact]
    public async Task RequestAsync_SetsAwaitingThenRunning_AndReturnsDecision()
    {
        var store = new InMemoryRunStore();
        var gate = new ApprovalGate();
        var rec = store.Create("agent", "a", "in");
        var handler = new PendingApprovalHandler(rec.Id, gate, store);

        var task = handler.RequestAsync(new ApprovalRequest { AgentId = "a", FunctionName = "f" });
        Assert.Equal(RunStatus.AwaitingApproval, store.Get(rec.Id)!.Status);

        gate.Resolve(gate.Pending(rec.Id)[0].Id, approved: true);
        Assert.True(await task);
        Assert.Equal(RunStatus.Running, store.Get(rec.Id)!.Status);
    }
}
