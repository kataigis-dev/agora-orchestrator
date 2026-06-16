using Agora.HumanInTheLoop;
using Agora.Runs;
using Xunit;

namespace Agora.Tests.Runs;

public class ApprovalGateTests
{
    private static ApprovalRequest Req() =>
        new() { AgentId = "a", FunctionName = "write_file", Arguments = "{}" };

    [Fact]
    public async Task WaitAsync_ParksUntilResolved()
    {
        var gate = new ApprovalGate();
        var task = gate.WaitAsync("run1", Req(), CancellationToken.None);
        Assert.False(task.IsCompleted);

        var pending = gate.Pending("run1");
        Assert.Single(pending);
        Assert.Equal("write_file", pending[0].FunctionName);

        Assert.True(gate.Resolve(pending[0].Id, approved: true));
        Assert.True(await task);
        Assert.Empty(gate.Pending("run1"));
    }

    [Fact]
    public async Task Resolve_False_ReturnsFalse()
    {
        var gate = new ApprovalGate();
        var task = gate.WaitAsync("run1", Req(), CancellationToken.None);
        gate.Resolve(gate.Pending("run1")[0].Id, approved: false);
        Assert.False(await task);
    }

    [Fact]
    public void Resolve_UnknownId_ReturnsFalse() => Assert.False(new ApprovalGate().Resolve("nope", true));

    [Fact]
    public void Pending_IsScopedByRunId()
    {
        var gate = new ApprovalGate();
        gate.WaitAsync("run1", Req(), CancellationToken.None);
        gate.WaitAsync("run2", Req(), CancellationToken.None);
        Assert.Single(gate.Pending("run1"));
        Assert.Single(gate.Pending("run2"));
    }
}
