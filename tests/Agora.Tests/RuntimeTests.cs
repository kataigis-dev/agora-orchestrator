using Agora;
using Agora.Providers;
using Xunit;

namespace Agora.Tests;

public class RuntimeTests
{
    private const string Config = """
        defaults: { model: balanced, timeout: 60 }
        providers:
          anthropic: { api_key_env: ANTHROPIC_API_KEY }
        models:
          balanced: { provider: anthropic, model: claude-sonnet-4-6 }
          smart: { provider: anthropic, model: claude-opus-4-8 }
        agents:
          planner: { model: smart, role: "You plan.", timeout: 300 }
          writer: { role: "You write." }
        """;

    private static Runtime Build(FakeChatProvider provider)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, Config);
        return Runtime.FromConfig(path, provider);
    }

    [Fact]
    public async Task RunAgent_RunsNamedAgent()
    {
        var provider = new FakeChatProvider(new[] { "planned!" });
        var result = await Build(provider).RunAgentAsync("planner", "do a thing");
        Assert.Equal("planned!", result.Output);
    }

    [Fact]
    public async Task RunAgent_UsesAgentModelAlias()
    {
        var provider = new FakeChatProvider(new[] { "x" });
        await Build(provider).RunAgentAsync("planner", "hi");
        Assert.Equal("smart", provider.Calls[0].Spec.Alias);
        Assert.Equal("claude-opus-4-8", provider.Calls[0].Spec.Model);
    }

    [Fact]
    public async Task RunAgent_AppliesPerAgentTimeoutOverride()
    {
        var provider = new FakeChatProvider(new[] { "x" });
        await Build(provider).RunAgentAsync("planner", "hi");
        Assert.Equal(300, provider.Calls[0].Spec.Timeout);
    }

    [Fact]
    public async Task RunAgent_FallsBackToDefaultModel()
    {
        var provider = new FakeChatProvider(new[] { "x" });
        await Build(provider).RunAgentAsync("writer", "hi");
        Assert.Equal("balanced", provider.Calls[0].Spec.Alias);
    }

    [Fact]
    public void BuildAgent_UnknownAgent_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => Build(new FakeChatProvider()).BuildAgent("ghost"));
    }
}
