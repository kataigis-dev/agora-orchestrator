using Agora;
using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Xunit;

namespace Agora.Tests;

public class RuntimeGraphTests
{
    private const string GraphConfig = """
        defaults: { model: balanced }
        providers:
          anthropic: { api_key_env: ANTHROPIC_API_KEY }
        models:
          balanced: { provider: anthropic, model: claude-sonnet-4-6 }
        agents:
          planner: { model: balanced, role: "Plan." }
          writer:  { model: balanced, role: "Write." }
        graph:
          entry: planner
          edges:
            - { from: planner, to: writer, type: handoff }
            - { from: writer,  to: END,    type: sequential }
        """;

    private const string NoGraphConfig = """
        models: { balanced: { provider: anthropic, model: claude-sonnet-4-6 } }
        agents: { writer: { model: balanced } }
        """;

    private static Runtime Build(string configText, FakeChatProvider provider)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, configText);
        return Runtime.FromConfig(path, provider);
    }

    [Fact]
    public async Task Run_ExecutesGraph_ReturnsFinalOutput()
    {
        var provider = new FakeChatProvider(new[] { "PLAN", "FINAL" });
        var result = await Build(GraphConfig, provider).RunAsync("do the task");

        Assert.Equal("FINAL", result.Output);
        Assert.Equal("PLAN", result.State.Outputs["planner"]);
        Assert.Equal("FINAL", result.State.Outputs["writer"]);
        Assert.Equal("PLAN\n\ndo the task", provider.Calls[1].Messages[^1].Content);
    }

    [Fact]
    public async Task Run_WithoutGraph_ThrowsGraphError()
    {
        var provider = new FakeChatProvider(new[] { "x" });
        var rt = Build(NoGraphConfig, provider);
        await Assert.ThrowsAsync<GraphError>(() => rt.RunAsync("hi"));
    }
}
