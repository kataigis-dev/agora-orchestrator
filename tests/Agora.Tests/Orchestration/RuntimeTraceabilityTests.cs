using Agora;
using Agora.Providers;
using Agora.Specs;

namespace Agora.Tests.Orchestration;

public class RuntimeTraceabilityTests
{
    private const string GraphConfig = """
        defaults: { model: balanced }
        providers:
          anthropic: { api_key_env: ANTHROPIC_API_KEY }
        models:
          balanced: { provider: anthropic, model: claude-sonnet-4-6 }
        spec:
          enabled: true
          store: { type: file, path: ./spec.json }
        agents:
          planner: { model: balanced, role: "Plan." }
          writer:  { model: balanced, role: "Write." }
        graph:
          entry: planner
          edges:
            - { from: planner, to: writer, type: handoff }
            - { from: writer,  to: END,    type: sequential }
        """;

    private static Runtime Build(SpecDocument seed)
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var configPath = Path.Combine(dir, "agora.yaml");
        File.WriteAllText(configPath, GraphConfig);
        File.WriteAllText(Path.Combine(dir, "spec.json"), SpecSerializer.Serialize(seed));
        return Runtime.FromConfig(configPath, new FakeChatProvider(new[] { "PLAN", "FINAL" }));
    }

    [Fact]
    public async Task RunResult_SurfacesTraceability_FromConfiguredSpecStore()
    {
        var seed = SpecDocument.Empty
            .WithRequirement(new Requirement
            {
                Id = "R1",
                Title = "Login",
                Status = RequirementStatus.Verified,
                AcceptanceCriteria = new[] { new AcceptanceCriterion("R1.A1", "works", new SpecCheck(CheckKind.Test, "test")) },
            })
            .WithTask(new TaskItem { Id = "T1", Description = "do it", RequirementIds = new[] { "R1" } });

        var result = await Build(seed).RunAsync("go");

        var t = result.Metrics!.Traceability;
        Assert.NotNull(t);
        Assert.Equal(1, t!.Requirements);
        Assert.Equal(1, t.Verified);
        Assert.Equal(1, t.Covered);
        Assert.True(t.Complete);
    }
}
