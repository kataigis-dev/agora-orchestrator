using Agora;
using Agora.Providers;
using Xunit;

namespace Agora.Tests;

public class RuntimeH2cTests
{
    // No `communication:` key → default h2c. Writer emits a benign block; critic emits [STATE:DONE]
    // which the H2cInterpreter turns into signal `done` → conditional edge to END.
    private const string Config = """
        defaults: { model: balanced }
        providers: { openai: { api_key_env: OPENAI_API_KEY } }
        models: { balanced: { provider: openai, model: gpt-4o } }
        agents:
          writer: { role: "write" }
          critic: { role: "review" }
        graph:
          entry: writer
          edges:
            - { from: writer, to: critic, type: sequential }
            - { from: critic, to: END,    type: conditional, when: done }
            - { from: critic, to: writer, type: conditional, when: fix, max_loops: 1 }
        """;

    private static Runtime Build(FakeChatProvider provider)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, Config);
        return Runtime.FromConfig(path, provider);
    }

    [Fact]
    public async Task H2cMode_RoutesOnBlockSubtype_AndInjectsPreamble()
    {
        var provider = new FakeChatProvider(new[] { "[BUILD:EXEC]\nstep:1", "[STATE:DONE]" });
        var result = await Build(provider).RunAsync("do it");

        Assert.Equal("[STATE:DONE]", result.Output);     // H2C passed through unchanged
        Assert.Equal("critic", result.State.LastAgent);  // routed critic -> END on `done`

        // The H2C preamble is prepended to each agent's system prompt.
        var system = provider.Calls[0].Messages[0];
        Assert.Equal("system", system.Role);
        Assert.Contains("H2C", system.Content);
    }
}
