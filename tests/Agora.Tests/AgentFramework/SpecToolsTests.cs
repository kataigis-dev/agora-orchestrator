using System.Text.Json;
using Agora.AgentFramework;
using Agora.Specs;
using Microsoft.Extensions.AI;

namespace Agora.Tests.AgentFramework;

public class SpecToolsTests
{
    private sealed class MemSpecStore : ISpecStore
    {
        public SpecDocument Doc = SpecDocument.Empty;
        public Task<SpecDocument> LoadAsync(CancellationToken ct = default) => Task.FromResult(Doc);
        public Task SaveAsync(SpecDocument d, CancellationToken ct = default) { Doc = d; return Task.CompletedTask; }
    }

    private static AIFunctionArguments Args(params (string Key, object? Value)[] items)
    {
        var args = new AIFunctionArguments();
        foreach (var (k, v) in items) args[k] = v;
        return args;
    }

    private static string AsString(object? result) =>
        result is JsonElement je ? je.GetString() ?? "" : result?.ToString() ?? "";

    private static AIFunction Tool(IEnumerable<AITool> tools, string name) =>
        tools.OfType<AIFunction>().First(t => t.Name == name);

    private static List<AITool> AllTools(ISpecStore store, bool requireCriteria = true) =>
        SpecTools.Create(
            new[] { "spec_get", "spec_propose_requirement", "spec_set_status", "spec_add_task", "spec_link_task" },
            store, requireCriteria);

    [Fact]
    public async Task ProposeRequirement_AssignsId_AndPersists()
    {
        var store = new MemSpecStore();
        var tools = AllTools(store);

        var result = AsString(await Tool(tools, "spec_propose_requirement").InvokeAsync(Args(
            ("title", "Login"), ("description", "user can log in"), ("priority", "must"),
            ("acceptance", "Given creds When submit Then a session exists"))));

        Assert.Contains("R1", result);
        var r = store.Doc.FindRequirement("R1")!;
        Assert.Equal(RequirementPriority.Must, r.Priority);
        Assert.Equal(RequirementStatus.Proposed, r.Status);
        Assert.Single(r.AcceptanceCriteria);
        Assert.Equal("R1.A1", r.AcceptanceCriteria[0].Id);
    }

    [Fact]
    public async Task ProposeRequirement_WithoutCriteria_IsRejectedWhenRequired()
    {
        var store = new MemSpecStore();
        var tools = AllTools(store, requireCriteria: true);

        var result = AsString(await Tool(tools, "spec_propose_requirement").InvokeAsync(Args(
            ("title", "X"), ("description", ""), ("priority", "must"), ("acceptance", ""))));

        Assert.Contains("rejected", result);
        Assert.Empty(store.Doc.Requirements); // nothing saved
    }

    [Fact]
    public async Task AddTask_LinkingUnknownRequirement_IsRejected()
    {
        var store = new MemSpecStore();
        var tools = AllTools(store);

        var result = AsString(await Tool(tools, "spec_add_task").InvokeAsync(Args(
            ("description", "do"), ("kind", "be"), ("requirementIds", "R9"))));

        Assert.Contains("rejected", result);
        Assert.Empty(store.Doc.Tasks);
    }

    [Fact]
    public async Task FullFlow_Propose_Task_Status_Get()
    {
        var store = new MemSpecStore();
        var tools = AllTools(store);

        await Tool(tools, "spec_propose_requirement").InvokeAsync(Args(
            ("title", "Login"), ("description", ""), ("priority", "must"), ("acceptance", "session exists")));
        var added = AsString(await Tool(tools, "spec_add_task").InvokeAsync(Args(
            ("description", "implement login"), ("kind", "be"), ("requirementIds", "R1"))));
        Assert.Contains("T1", added);

        var status = AsString(await Tool(tools, "spec_set_status").InvokeAsync(Args(
            ("requirementId", "R1"), ("status", "approved"))));
        Assert.Contains("Approved", status);

        var view = AsString(await Tool(tools, "spec_get").InvokeAsync(Args()));
        Assert.Contains("R1", view);
        Assert.Contains("Approved", view);
        Assert.Contains("T1", view);
    }

    [Fact]
    public void Create_RequiresStore_AndAllowList()
    {
        Assert.Empty(SpecTools.Create(new[] { "spec_get" }, store: null, requireCriteria: true));
        Assert.Empty(SpecTools.Create(Array.Empty<string>(), new MemSpecStore(), requireCriteria: true));
    }
}
