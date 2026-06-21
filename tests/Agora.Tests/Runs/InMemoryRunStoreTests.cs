using Agora.Runs.Contracts;
using Agora.Runs.Models;
using Agora.Runs.Concretes;
using Xunit;

namespace Agora.Tests.Runs;

public class InMemoryRunStoreTests
{
    [Fact]
    public void Create_ThenGet_ReturnsRunningRecord()
    {
        var store = new InMemoryRunStore();
        var rec = store.Create(mode: "agent", agentId: "writer", input: "hi");
        Assert.False(string.IsNullOrEmpty(rec.Id));
        Assert.Equal(RunStatus.Running, rec.Status);
        Assert.Equal("writer", store.Get(rec.Id)!.AgentId);
    }

    [Fact]
    public void Update_MutatesRecord()
    {
        var store = new InMemoryRunStore();
        var rec = store.Create("graph", null, "go");
        store.Update(rec.Id, r => { r.Status = RunStatus.Completed; r.Output = "done"; });
        var got = store.Get(rec.Id)!;
        Assert.Equal(RunStatus.Completed, got.Status);
        Assert.Equal("done", got.Output);
    }

    [Fact]
    public void Get_UnknownId_ReturnsNull() => Assert.Null(new InMemoryRunStore().Get("nope"));
}
