using Agora.Cli;
using Agora.Providers;
using Xunit;

namespace Agora.Tests.Cli;

public class CliRunGraphTests
{
    [Fact]
    public void RunGraph_WithFakeProvider_PrintsOutput()
    {
        var config = """
            models: { balanced: { provider: openai, model: gpt-4o } }
            agents:
              planner: { model: balanced }
              writer:  { model: balanced }
            graph:
              entry: planner
              edges:
                - { from: planner, to: writer, type: handoff }
                - { from: writer,  to: END,    type: sequential }
            """;
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, config);

        var outw = new StringWriter();
        var provider = new FakeChatProvider(new[] { "PLAN", "DONE" });
        var code = CliRunner.Run(
            new[] { "run", "--graph", "--config", path, "--input", "hello" }, provider, outw, new StringWriter());

        Assert.Equal(0, code);
        Assert.Contains("DONE", outw.ToString());
    }
}
