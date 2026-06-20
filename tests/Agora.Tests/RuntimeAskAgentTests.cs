using Agora;
using Agora.Agents;
using Agora.Providers;
using Xunit;

namespace Agora.Tests;

public class RuntimeAskAgentTests
{
    private sealed class CapturingFactory : IToolAgentFactory
    {
        public List<AgentBuildContext> Contexts { get; } = new();
        public IAgent Create(AgentBuildContext context)
        {
            Contexts.Add(context);
            return new StubAgent();
        }

        private sealed class StubAgent : IAgent
        {
            public Task<AgentResult> RunAsync(string userInput, string context = "")
                => Task.FromResult(new AgentResult { Output = "stub" });
        }
    }

    private static Runtime Build(FakeChatProvider provider, CapturingFactory factory, bool answererTooled)
    {
        var answerer = answererTooled
            ? "answerer: { model: balanced, tools: [ask_agent], role: \"Answer.\" }"
            : "answerer: { model: balanced, role: \"Answer.\" }";
        var yaml = $$"""
            providers:
              openai: { api_key_env: OPENAI_API_KEY }
            models:
              balanced: { provider: openai, model: gpt-4o }
            agents:
              asker: { model: balanced, tools: [ask_agent], role: "Ask." }
              {{answerer}}
            """;
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, yaml);
        return Runtime.FromConfig(path, provider, toolAgentFactory: factory);
    }

    [Fact]
    public async Task AskAgent_Callback_RunsTargetAgent()
    {
        var factory = new CapturingFactory();
        var rt = Build(new FakeChatProvider(new[] { "ANSWER FROM A" }), factory, answererTooled: false);

        rt.BuildAgent("asker");
        var askerCtx = factory.Contexts.Single(c => c.Card.Id == "asker");
        Assert.NotNull(askerCtx.AskAgent);

        // answerer is plain → built as the core Agent → returns the provider's response.
        var answer = await askerCtx.AskAgent!("answerer", "what is X");
        Assert.Equal("ANSWER FROM A", answer);
    }

    [Fact]
    public async Task AskAgent_UnknownTarget_ReturnsError()
    {
        var factory = new CapturingFactory();
        var rt = Build(new FakeChatProvider(new[] { "x" }), factory, answererTooled: false);
        rt.BuildAgent("asker");
        var askerCtx = factory.Contexts.Single(c => c.Card.Id == "asker");

        var answer = await askerCtx.AskAgent!("ghost", "q");
        Assert.Contains("unknown agent", answer);
    }

    [Fact]
    public async Task AskAgent_AnswerMode_TargetCannotAskBack()
    {
        var factory = new CapturingFactory();
        var rt = Build(new FakeChatProvider(new[] { "stub" }), factory, answererTooled: true);

        rt.BuildAgent("asker");
        var askerCtx = factory.Contexts.Single(c => c.Card.Id == "asker");

        // Asking a tooled agent routes it through the factory in answer-mode → no AskAgent.
        await askerCtx.AskAgent!("answerer", "q");
        var answererCtx = factory.Contexts.Last(c => c.Card.Id == "answerer");
        Assert.Null(answererCtx.AskAgent);
    }
}
