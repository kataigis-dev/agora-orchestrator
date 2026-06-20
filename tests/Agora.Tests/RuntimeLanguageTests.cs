using Agora;
using Agora.Agents;
using Agora.Providers;
using Xunit;

namespace Agora.Tests;

public class RuntimeLanguageTests
{
    private sealed class CapturingFactory : IToolAgentFactory
    {
        public AgentBuildContext? Last { get; private set; }
        public IAgent Create(AgentBuildContext context)
        {
            Last = context;
            return new Stub();
        }

        private sealed class Stub : IAgent
        {
            public Task<AgentResult> RunAsync(string userInput, string context = "", Action<string>? onChunk = null)
                => Task.FromResult(new AgentResult { Output = "x" });
        }
    }

    private static AgentBuildContext BuildWriter(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, yaml);
        try
        {
            var factory = new CapturingFactory();
            Runtime.FromConfig(path, new FakeChatProvider(new[] { "x" }), toolAgentFactory: factory)
                .BuildAgent("writer");
            return factory.Last!;
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Language_IsInjectedIntoSystemPrompt()
    {
        var ctx = BuildWriter("""
            language: English
            providers: { openai: { api_key_env: K } }
            models: { balanced: { provider: openai, model: gpt-4o } }
            agents:
              writer: { model: balanced, role: "Write things.", tools: [rag_search] }
            """);
        Assert.Contains("OUTPUT LANGUAGE", ctx.Card.SystemPrompt);
        Assert.Contains("English", ctx.Card.SystemPrompt);
    }

    [Fact]
    public void NoLanguage_NoInjection()
    {
        var ctx = BuildWriter("""
            providers: { openai: { api_key_env: K } }
            models: { balanced: { provider: openai, model: gpt-4o } }
            agents:
              writer: { model: balanced, role: "Write things.", tools: [rag_search] }
            """);
        Assert.DoesNotContain("OUTPUT LANGUAGE", ctx.Card.SystemPrompt);
    }
}
