using System.Text;
using Agora;
using Agora.Agents;
using Agora.Orchestration;
using Agora.Providers;
using Xunit;

namespace Agora.Tests;

public class StreamingTests
{
    private static ModelSpec Spec() => new()
    {
        Alias = "m", Provider = "x", Model = "x", Temperature = 0, MaxTokens = 50, Timeout = 5, Retries = 0,
    };

    private static Func<string, Agent> Factory(Dictionary<string, FakeChatProvider> providers) =>
        id => new Agent(new AgentCard { Id = id, Model = "m" }, providers[id], Spec());

    private static Graph Graph2() => new()
    {
        Entry = "planner",
        Nodes = new Dictionary<string, Node> { ["planner"] = new("planner"), ["writer"] = new("writer") },
        Edges = new[] { new Edge("planner", "writer"), new Edge("writer", "END") },
    };

    [Fact]
    public async Task FakeProvider_Streams_Chunks_AndReturnsFullResult()
    {
        var provider = new FakeChatProvider(new[] { "hello world foo" });
        var chunks = new List<string>();
        var result = await provider.StreamAsync(
            new[] { new ChatMessage("user", "x") }, Spec(), chunk => chunks.Add(chunk));

        Assert.Equal("hello world foo", result.Text);
        Assert.Equal("hello world foo", string.Concat(chunks));
        Assert.True(chunks.Count >= 3);
    }

    [Fact]
    public async Task Agent_StreamsTokens_WhenSinkProvided()
    {
        var agent = new Agent(new AgentCard { Id = "a", Model = "m" },
            new FakeChatProvider(new[] { "the answer is here" }), Spec());
        var sb = new StringBuilder();

        var result = await agent.RunAsync("q", "", chunk => sb.Append(chunk));

        Assert.Equal("the answer is here", result.Output);
        Assert.Equal("the answer is here", sb.ToString());
    }

    [Fact]
    public async Task GraphExecutor_StreamsEachAgentsTokens()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["planner"] = new(new[] { "PLAN HERE" }),
            ["writer"] = new(new[] { "FINAL TEXT" }),
        };
        var sb = new StringBuilder();

        await new GraphExecutor(Graph2(), Factory(providers), onChunk: chunk => sb.Append(chunk)).RunAsync("task");

        var streamed = sb.ToString();
        Assert.Contains("PLAN HERE", streamed);
        Assert.Contains("FINAL TEXT", streamed);
    }

    [Fact]
    public async Task Runtime_RunAgent_Streams_ThroughResilientWrapper()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, """
            providers: { openai: { api_key_env: K } }
            models: { balanced: { provider: openai, model: gpt-4o } }
            agents: { writer: { model: balanced, role: "Write." } }
            """);
        var runtime = Runtime.FromConfig(path, new FakeChatProvider(new[] { "streamed output text" }));
        var sb = new StringBuilder();

        await runtime.RunAgentAsync("writer", "go", chunk => sb.Append(chunk));

        Assert.Equal("streamed output text", sb.ToString());
        File.Delete(path);
    }
}
