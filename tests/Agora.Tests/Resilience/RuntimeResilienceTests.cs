using Agora;
using Agora.Tests.Providers;
using Xunit;

namespace Agora.Tests.Resilience;

public class RuntimeResilienceTests
{
    private const string Config = """
        defaults: { model: balanced, timeout: 30, retries: 2, retry_base_delay: 0 }
        providers:
          openai: { api_key_env: OPENAI_API_KEY }
        models:
          balanced: { provider: openai, model: gpt-4o }
        agents:
          writer: { role: "You write." }
        """;

    [Fact]
    public async Task RunAgent_RetriesTransientProviderFailures()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, Config);
        var flaky = new FlakyChatProvider(failures: 2, text: "written");

        var result = await Runtime.FromConfig(path, flaky).RunAgentAsync("writer", "do it");

        Assert.Equal("written", result.Output);
        Assert.Equal(3, flaky.Calls);
    }
}
