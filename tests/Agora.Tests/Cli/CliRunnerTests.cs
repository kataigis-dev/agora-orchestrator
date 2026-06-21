using Agora.Cli;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Xunit;

namespace Agora.Tests.Cli;

public class CliRunnerTests
{
    private const string Config = """
        providers:
          anthropic: { api_key_env: ANTHROPIC_API_KEY }
        models:
          balanced: { provider: anthropic, model: claude-sonnet-4-6 }
        agents:
          writer: { model: balanced, role: "You write." }
        """;

    private static string WriteConfig()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, Config);
        return path;
    }

    [Fact]
    public void Validate_Ok()
    {
        var outw = new StringWriter();
        var code = CliRunner.Run(new[] { "validate", "--config", WriteConfig() }, null, outw, new StringWriter());
        Assert.Equal(0, code);
        Assert.Contains("OK", outw.ToString());
    }

    [Fact]
    public void Validate_BadConfig_ReturnsError()
    {
        var bad = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(bad, "agents: {}\n");
        var errw = new StringWriter();
        var code = CliRunner.Run(new[] { "validate", "--config", bad }, null, new StringWriter(), errw);
        Assert.Equal(1, code);
        Assert.Contains("INVALID", errw.ToString());
    }

    [Fact]
    public void Run_WithFakeProvider_PrintsOutput()
    {
        var outw = new StringWriter();
        var provider = new FakeChatProvider(new[] { "CLI OUTPUT" });
        var code = CliRunner.Run(
            new[] { "run", "--config", WriteConfig(), "--agent", "writer", "--input", "hello" },
            provider, outw, new StringWriter());
        Assert.Equal(0, code);
        Assert.Contains("CLI OUTPUT", outw.ToString());
    }
}
