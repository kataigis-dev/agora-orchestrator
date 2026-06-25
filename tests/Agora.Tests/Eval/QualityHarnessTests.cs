using Agora.Cli;
using Agora.Eval.Quality;
using Agora.Providers.Concretes;
using Agora.Providers.Models;
using Xunit;

namespace Agora.Tests.Eval;

public class QualityHarnessTests
{
    // A minimal graph config; the SUT provider is a fake, so the actual output text is irrelevant to the
    // judge tests (they use a FakeJudge) — only that the run produces an output.
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

    private static string WriteConfig()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, Config);
        return path;
    }

    private static ModelSpec JudgeSpec() => new() { Alias = "j", Provider = "openai", Model = "gpt-4o" };

    private static JudgeVerdict Verdict(params (string Dim, double Score)[] dims) => new()
    {
        Valid = true,
        Dimensions = dims.ToDictionary(d => d.Dim, d => new DimensionVerdict(d.Score, "", "")),
    };

    // ---- LlmJudge parsing (constrained JSON + fail-closed) -------------------------------------

    [Fact]
    public async Task LlmJudge_ParsesValidJson()
    {
        const string json = """
            { "correctness": { "justification": "ok", "evidence": "l1", "score": 1 },
              "relevance":   { "justification": "ok", "evidence": "x",  "score": 0.5 } }
            """;
        var judge = new LlmJudge(new FakeChatProvider(new[] { json }), JudgeSpec());
        var verdict = await judge.JudgeAsync(Request("correctness", "relevance"));

        Assert.True(verdict.Valid);
        Assert.Equal(1.0, verdict.Dimensions["correctness"].Score);
        Assert.Equal(0.5, verdict.Dimensions["relevance"].Score);
    }

    [Fact]
    public async Task LlmJudge_UnparseableReply_FailsClosed()
    {
        var judge = new LlmJudge(new FakeChatProvider(new[] { "I think it's pretty good honestly!" }), JudgeSpec());
        var verdict = await judge.JudgeAsync(Request("correctness"));

        Assert.False(verdict.Valid);
        Assert.NotNull(verdict.Error);
    }

    [Fact]
    public async Task LlmJudge_MissingDimension_FailsClosed()
    {
        const string json = """{ "correctness": { "score": 1 } }""";
        var judge = new LlmJudge(new FakeChatProvider(new[] { json }), JudgeSpec());
        var verdict = await judge.JudgeAsync(Request("correctness", "relevance"));

        Assert.False(verdict.Valid);
    }

    [Fact]
    public async Task LlmJudge_OutOfScaleScore_FailsClosed()
    {
        const string json = """{ "correctness": { "score": 0.7 } }""";
        var judge = new LlmJudge(new FakeChatProvider(new[] { json }), JudgeSpec());
        var verdict = await judge.JudgeAsync(Request("correctness"));

        Assert.False(verdict.Valid);
    }

    [Fact]
    public async Task LlmJudge_MarkerFallback_WhenNoJson()
    {
        var judge = new LlmJudge(new FakeChatProvider(new[] { "correctness: 1\nrelevance: 0.5" }), JudgeSpec());
        var verdict = await judge.JudgeAsync(Request("correctness", "relevance"));

        Assert.True(verdict.Valid);
        Assert.Equal(1.0, verdict.Dimensions["correctness"].Score);
        Assert.Equal(0.5, verdict.Dimensions["relevance"].Score);
    }

    // ---- QualityRunner (gate → judge → aggregate) ---------------------------------------------

    [Fact]
    public async Task Runner_AggregatesUnweightedMean()
    {
        var config = WriteConfig();
        var scenario = new QualityScenario
        {
            Id = "agg",
            Input = "go",
            Config = config,
            Rubric = new() { ["correctness"] = "c", ["completeness"] = "cp", ["relevance"] = "r" },
        };
        var judge = new FakeJudge(new[] { Verdict(("correctness", 1), ("completeness", 0.5), ("relevance", 0)) });

        var result = await QualityRunner.RunScenarioAsync(scenario, new FakeChatProvider(), judge);

        Assert.True(result.DeterministicPassed);
        Assert.Null(result.Error);
        Assert.Equal(0.5, result.Aggregate);            // (1 + 0.5 + 0) / 3
        Assert.Equal(3, result.Dimensions.Count);
        Assert.Single(judge.Requests);
        File.Delete(config);
    }

    [Fact]
    public async Task Runner_DeterministicGateFails_SkipsJudge_ScoresZero()
    {
        var config = WriteConfig();
        var scenario = new QualityScenario
        {
            Id = "gate",
            Input = "go",
            Config = config,
            Deterministic = { new DeterministicCheck { Kind = "contains", Value = "NOT-IN-OUTPUT" } },
            Rubric = new() { ["correctness"] = "c" },
        };
        var judge = new FakeJudge(new[] { Verdict(("correctness", 1)) });

        var result = await QualityRunner.RunScenarioAsync(scenario, new FakeChatProvider(), judge);

        Assert.False(result.DeterministicPassed);
        Assert.Equal(0, result.Aggregate);
        Assert.Empty(judge.Requests);                   // hard gate short-circuits the LLM call
        File.Delete(config);
    }

    [Fact]
    public async Task Runner_InvalidVerdict_BecomesEvaluationError_NotPass()
    {
        var config = WriteConfig();
        var scenario = new QualityScenario
        {
            Id = "failclosed",
            Input = "go",
            Config = config,
            Rubric = new() { ["correctness"] = "c" },
        };
        var judge = new FakeJudge(@default: JudgeVerdict.Invalid("garbage"));

        var result = await QualityRunner.RunScenarioAsync(scenario, new FakeChatProvider(), judge);

        Assert.NotNull(result.Error);
        Assert.Null(result.Aggregate);                  // excluded from the suite mean, never a pass
        File.Delete(config);
    }

    [Fact]
    public async Task Runner_LeanScenario_CarriesMetrics()
    {
        var config = WriteConfig();
        var scenario = new QualityScenario
        {
            Id = "lean",
            Input = "go",
            Config = config,
            Graph = false,                              // single-agent run (no graph)
            Agent = "writer",
            Rubric = new() { ["correctness"] = "c" },
        };
        var judge = new FakeJudge(new[] { Verdict(("correctness", 1)) });

        var result = await QualityRunner.RunScenarioAsync(scenario, new FakeChatProvider(), judge);

        Assert.Null(result.Error);
        Assert.Equal(1.0, result.Aggregate);
        Assert.NotNull(result.Metrics);                 // graph=false runs now carry RunMetrics (was metrics: null)
        Assert.Equal(1, result.Metrics!.Steps);
        File.Delete(config);
    }

    [Fact]
    public async Task Suite_AggregatesAndReportsJudgeHumanAgreement()
    {
        var config = WriteConfig();
        var s1 = new QualityScenario { Id = "s1", Input = "go", Config = config, ExpectedScore = 1.0,
            Rubric = new() { ["correctness"] = "c" } };
        var s2 = new QualityScenario { Id = "s2", Input = "go", Config = config, ExpectedScore = 1.0,
            Rubric = new() { ["correctness"] = "c" } };
        var judge = new FakeJudge(new[] { Verdict(("correctness", 1)), Verdict(("correctness", 0.5)) });

        var suite = await QualityRunner.RunSuiteAsync(new[] { s1, s2 }, new FakeChatProvider(), judge);

        Assert.Equal(0.75, suite.SuiteScore);           // (1 + 0.5) / 2
        Assert.Equal(0.25, suite.JudgeHumanMae);        // (|1-1| + |0.5-1|) / 2
        Assert.Equal(2, suite.LabeledCases);
        Assert.Equal(0, suite.ErrorCases);
        Assert.False(string.IsNullOrWhiteSpace(QualityReport.Human(suite)));
        Assert.Contains("suiteScore", QualityReport.Json(suite));
        File.Delete(config);
    }

    // ---- CLI gate -----------------------------------------------------------------------------

    [Fact]
    public void EvalQuality_RefusesWithoutLiveFlag()
    {
        var prior = Environment.GetEnvironmentVariable("AGORA_EVAL_LIVE");
        Environment.SetEnvironmentVariable("AGORA_EVAL_LIVE", null);
        try
        {
            var err = new StringWriter();
            var code = CliRunner.Run(
                new[] { "eval-quality", "--suite", "x", "--judge-config", "y" },
                provider: new FakeChatProvider(), @out: new StringWriter(), error: err);

            Assert.Equal(2, code);
            Assert.Contains("AGORA_EVAL_LIVE", err.ToString());
        }
        finally
        {
            Environment.SetEnvironmentVariable("AGORA_EVAL_LIVE", prior);
        }
    }

    private static JudgeRequest Request(params string[] dims) => new()
    {
        Input = "in",
        Output = "out",
        Rubric = dims.ToDictionary(d => d, d => "criteria"),
    };
}
