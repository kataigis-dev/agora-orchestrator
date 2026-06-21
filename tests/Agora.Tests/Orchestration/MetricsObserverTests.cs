using Agora;
using Agora.Orchestration;
using Agora.Providers;
using Xunit;

namespace Agora.Tests.Orchestration;

public class MetricsObserverTests
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

    private static Runtime Build(FakeChatProvider provider)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, GraphConfig);
        return Runtime.FromConfig(path, provider);
    }

    [Fact]
    public async Task RunResult_ExposesMetrics_StepsTokensAndCacheHits()
    {
        var provider = new FakeChatProvider(new[] { "PLAN", "FINAL" });

        var result = await Build(provider).RunAsync("do the task");

        var m = result.Metrics;
        Assert.NotNull(m);
        Assert.True(m!.Completed);
        Assert.Equal(2, m.Steps);
        // FakeChatProvider returns 10 in / 5 out per call, and 7 cache-read because Agent marks the
        // system prompt as cacheable — so caching is measurable end to end.
        Assert.Equal(20, m.InputTokens);
        Assert.Equal(10, m.OutputTokens);
        Assert.Equal(14, m.CacheReadTokens);
        Assert.Equal(0.7, m.CacheHitRate, 3);
        Assert.Equal(0, m.ReworkCount);
        Assert.Equal(1, m.NodeVisits["planner"]);
    }

    [Fact]
    public void Observer_CountsReworkAndSignals_FromRevisits()
    {
        var graph = new Graph
        {
            Entry = "a",
            Nodes = new Dictionary<string, Node> { ["a"] = new("a") },
            Edges = Array.Empty<Edge>(),
        };
        var observer = new MetricsExecutionObserver();

        observer.OnGraphStart(graph);
        observer.OnNodeStart(1, "writer", "in");
        observer.OnSignals(new[] { "fix" });
        observer.OnNodeStart(2, "writer", "in");   // revisit → rework
        observer.OnSignals(new[] { "pass" });
        observer.OnUsage(new TokenUsage(100, 40, 30, 5));
        observer.OnGraphComplete();

        var m = observer.Metrics;
        Assert.Equal(2, m.Steps);
        Assert.Equal(2, m.NodeVisits["writer"]);
        Assert.Equal(1, m.ReworkCount);            // one re-execution beyond the first
        Assert.Equal(1, m.Signals["fix"]);
        Assert.Equal(1, m.Signals["pass"]);
        Assert.Equal(100, m.InputTokens);
        Assert.Equal(30, m.CacheReadTokens);
    }

    [Fact]
    public void Composite_FansOutEveryEvent()
    {
        var metrics = new MetricsExecutionObserver();
        var recorder = new RecordingObserver();
        var composite = new CompositeExecutionObserver(recorder, metrics);
        var graph = new Graph
        {
            Entry = "a",
            Nodes = new Dictionary<string, Node> { ["a"] = new("a") },
            Edges = Array.Empty<Edge>(),
        };

        composite.OnGraphStart(graph);
        composite.OnNodeStart(1, "a", "in");
        composite.OnUsage(new TokenUsage(10, 5, 0, 0));
        composite.OnGraphComplete();

        Assert.Equal(1, recorder.NodeStarts);
        Assert.True(recorder.Completed);
        Assert.Equal(10, metrics.Metrics.InputTokens);
    }

    private sealed class RecordingObserver : IExecutionObserver
    {
        public int NodeStarts { get; private set; }
        public bool Completed { get; private set; }
        public void OnGraphStart(Graph graph) { }
        public void OnNodeStart(int step, string nodeId, string input) => NodeStarts++;
        public void OnParallel(IReadOnlyList<string> branches) { }
        public void OnSignals(IReadOnlyCollection<string> signals) { }
        public void OnArtifact(string key, string value) { }
        public void OnEdge(string label, string target, bool isEnd) { }
        public void OnGraphComplete() => Completed = true;
    }
}
