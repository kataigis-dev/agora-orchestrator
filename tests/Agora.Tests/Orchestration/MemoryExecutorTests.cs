using Agora.Agents;
using Agora.Orchestration;
using Agora.Providers;
using Agora.Rag;
using Xunit;

namespace Agora.Tests.Orchestration;

public class MemoryExecutorTests
{
    private static ModelSpec Spec() => new()
    {
        Alias = "m", Provider = "x", Model = "x", Temperature = 0, MaxTokens = 50, Timeout = 5, Retries = 0,
    };

    private static Func<string, Agent> Factory(Dictionary<string, FakeChatProvider> providers) =>
        id => new Agent(new AgentCard { Id = id, Model = "m" }, providers[id], Spec());

    private static Graph HandoffGraph() => new()
    {
        Entry = "a",
        Nodes = new Dictionary<string, Node> { ["a"] = new("a"), ["b"] = new("b") },
        Edges = new[] { new Edge("a", "b", "handoff"), new Edge("b", "END") },
    };

    [Fact]
    public async Task RememberOutputs_MakesOutputRecallable_WhenInboxIsEmpty()
    {
        // Handoff mode + no declared artifact → b's inbox is empty; only RememberOutputs makes
        // a's output reach b (via recall), isolating the feature.
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["a"] = new(new[] { "paris is the capital of france" }),
            ["b"] = new(new[] { "DONE" }),
        };
        var memory = new ContextMemory(new FakeEmbedder(64), new InMemoryVectorStore());

        await new GraphExecutor(HandoffGraph(), Factory(providers), handoff: true, memory: memory,
            memoryOptions: new MemoryOptions(TopK: 5, RememberOutputs: true)).RunAsync("question about france");

        var bMsg = providers["b"].Calls[0].Messages[^1].Content;
        Assert.Contains("paris is the capital", bMsg);
    }

    [Fact]
    public async Task WithoutRememberOutputs_AndNoArtifacts_NextAgentGetsNothing()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["a"] = new(new[] { "paris is the capital of france" }),
            ["b"] = new(new[] { "DONE" }),
        };
        var memory = new ContextMemory(new FakeEmbedder(64), new InMemoryVectorStore());

        await new GraphExecutor(HandoffGraph(), Factory(providers), handoff: true, memory: memory,
            memoryOptions: new MemoryOptions(TopK: 5, RememberOutputs: false)).RunAsync("question about france");

        var bMsg = providers["b"].Calls[0].Messages[^1].Content;
        Assert.DoesNotContain("paris is the capital", bMsg);
    }
}
