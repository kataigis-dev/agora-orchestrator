using System.Text.Json;
using Agora.AgentFramework;
using Agora.Specs;
using Agora.Verification;
using Microsoft.Extensions.AI;

namespace Agora.Tests.AgentFramework;

public class CheckToolsTests
{
    private sealed class FakeRunner : ICheckRunner
    {
        private readonly Dictionary<string, bool> _outcomes;
        public FakeRunner(Dictionary<string, bool> outcomes) => _outcomes = outcomes;
        public string Workdir => Directory.GetCurrentDirectory();
        public bool HasCheck(string name) => _outcomes.ContainsKey(name);
        public Task<CheckResult> RunAsync(string name, IReadOnlyDictionary<string, string> args, CancellationToken ct = default)
            => Task.FromResult(new CheckResult(name, _outcomes[name], _outcomes[name] ? 0 : 1, "output", 1));
    }

    private sealed class MemSpecStore : ISpecStore
    {
        public SpecDocument Doc;
        public MemSpecStore(SpecDocument doc) => Doc = doc;
        public Task<SpecDocument> LoadAsync(CancellationToken ct = default) => Task.FromResult(Doc);
        public Task SaveAsync(SpecDocument d, CancellationToken ct = default) { Doc = d; return Task.CompletedTask; }
    }

    private static AIFunctionArguments Args(params (string Key, object? Value)[] items)
    {
        var a = new AIFunctionArguments();
        foreach (var (k, v) in items) a[k] = v;
        return a;
    }

    private static string AsString(object? result) =>
        result is JsonElement je ? je.GetString() ?? "" : result?.ToString() ?? "";

    private static AIFunction Tool(IEnumerable<AITool> tools, string name) =>
        tools.OfType<AIFunction>().First(t => t.Name == name);

    [Fact]
    public async Task RunCheck_ReturnsRealResult()
    {
        var tools = CheckTools.Create(new[] { "run_check" }, new FakeRunner(new() { ["build"] = true }), store: null, requireCriteria: true);
        var result = AsString(await Tool(tools, "run_check").InvokeAsync(Args(("name", "build"), ("args", ""))));
        Assert.Contains("PASS", result);
    }

    [Fact]
    public async Task RunCheck_UnknownCheck_IsRejected()
    {
        var tools = CheckTools.Create(new[] { "run_check" }, new FakeRunner(new() { ["build"] = true }), store: null, requireCriteria: true);
        var result = AsString(await Tool(tools, "run_check").InvokeAsync(Args(("name", "deploy"), ("args", ""))));
        Assert.Contains("unknown check", result);
    }

    [Fact]
    public async Task SpecVerify_MarksRequirementVerified_OnlyWhenChecksPass()
    {
        var doc = SpecDocument.Empty
            .WithRequirement(new Requirement
            {
                Id = "R1",
                Title = "Login",
                Status = RequirementStatus.Implemented,
                AcceptanceCriteria = new[] { new AcceptanceCriterion("R1.A1", "tested", new SpecCheck(CheckKind.Test, "test")) },
            })
            .WithTask(new TaskItem { Id = "T1", Description = "do", RequirementIds = new[] { "R1" } });
        var store = new MemSpecStore(doc);

        var passing = CheckTools.Create(new[] { "spec_verify" }, new FakeRunner(new() { ["test"] = true }), store, requireCriteria: true);
        var report = AsString(await Tool(passing, "spec_verify").InvokeAsync(Args(("requirementId", "R1"))));

        Assert.Contains("VERIFIED", report);
        Assert.Equal(RequirementStatus.Verified, store.Doc.FindRequirement("R1")!.Status);
        Assert.Contains(store.Doc.FindTask("T1")!.Evidence, e => e.Kind == "check");
    }

    [Fact]
    public async Task SpecVerify_FailingCheck_DoesNotAdvanceStatus()
    {
        var doc = SpecDocument.Empty.WithRequirement(new Requirement
        {
            Id = "R1",
            Title = "Login",
            Status = RequirementStatus.Implemented,
            AcceptanceCriteria = new[] { new AcceptanceCriterion("R1.A1", "tested", new SpecCheck(CheckKind.Test, "test")) },
        });
        var store = new MemSpecStore(doc);

        var failing = CheckTools.Create(new[] { "spec_verify" }, new FakeRunner(new() { ["test"] = false }), store, requireCriteria: true);
        var report = AsString(await Tool(failing, "spec_verify").InvokeAsync(Args(("requirementId", "R1"))));

        Assert.Contains("NOT VERIFIED", report);
        Assert.Equal(RequirementStatus.Implemented, store.Doc.FindRequirement("R1")!.Status);
    }

    [Fact]
    public void Tools_RequireTheirServices()
    {
        // run_check needs a runner; spec_verify needs runner + store.
        Assert.Empty(CheckTools.Create(new[] { "run_check" }, runner: null, store: null, requireCriteria: true));
        Assert.Empty(CheckTools.Create(new[] { "spec_verify" }, new FakeRunner(new()), store: null, requireCriteria: true));
    }
}
