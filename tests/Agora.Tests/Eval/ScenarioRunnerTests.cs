using Agora.Cli;
using Agora.Eval;
using Xunit;

namespace Agora.Tests.Eval;

public class ScenarioRunnerTests
{
    private const string Config = """
        communication: natural
        providers: { openai: { api_key_env: K } }
        models: { balanced: { provider: openai, model: gpt-4o } }
        agents:
          planner: { model: balanced, role: "Plan." }
          writer: { model: balanced, role: "Write." }
        graph:
          entry: planner
          edges:
            - { from: planner, to: writer, type: sequential }
            - { from: writer, to: END, type: sequential }
        """;

    private static string WriteFile(string content, string ext)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ext);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task PassingScenario_Passes()
    {
        var path = WriteFile(Config, ".yaml");
        var scenario = new Scenario
        {
            Input = "go",
            Responses = { "PLAN", "the FINAL answer <<signal done>>" },
            ExpectOutputContains = { "FINAL answer" },
            ExpectSignals = { "done" },
        };
        var result = await ScenarioRunner.RunAsync(path, scenario);
        Assert.True(result.Passed);
        Assert.Empty(result.Failures);
        File.Delete(path);
    }

    [Fact]
    public async Task FailingScenario_ReportsEachFailure()
    {
        var path = WriteFile(Config, ".yaml");
        var scenario = new Scenario
        {
            Input = "go",
            Responses = { "PLAN", "a result" },
            ExpectOutputContains = { "missing-thing" },
            ExpectSignals = { "approved" },
        };
        var result = await ScenarioRunner.RunAsync(path, scenario);
        Assert.False(result.Passed);
        Assert.Equal(2, result.Failures.Count);
        File.Delete(path);
    }

    [Fact]
    public void Eval_ViaCliRunner_Pass()
    {
        var configPath = WriteFile(Config, ".yaml");
        var scenarioPath = WriteFile(
            """{"input":"go","responses":["PLAN","FINAL <<signal done>>"],"expectOutputContains":["FINAL"],"expectSignals":["done"]}""",
            ".json");

        var outw = new StringWriter();
        var code = CliRunner.Run(
            new[] { "eval", "--config", configPath, "--scenario", scenarioPath },
            @out: outw, error: new StringWriter());

        Assert.Equal(0, code);
        Assert.Contains("PASS", outw.ToString());
        File.Delete(configPath);
        File.Delete(scenarioPath);
    }
}
