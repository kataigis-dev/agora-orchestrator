using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Xunit;

namespace Agora.Tests.Agents;

public class AgentTests
{
    private static ModelSpec Spec() => new()
    {
        Alias = "fast", Provider = "anthropic", Model = "claude-haiku-4-5",
        Temperature = 0.2, MaxTokens = 100, Timeout = 10, Retries = 0,
    };

    [Fact]
    public async Task Run_ReturnsOutputAndTokens()
    {
        var provider = new FakeChatProvider(new[] { "the answer" });
        var agent = new Agent(new AgentCard { Id = "writer", Model = "fast", Role = "You write." }, provider, Spec());
        var result = await agent.RunAsync("Summarize X");
        Assert.Equal("the answer", result.Output);
        Assert.Equal(10, result.InputTokens);
        Assert.Equal(5, result.OutputTokens);
    }

    [Fact]
    public async Task Run_BuildsSystemPromptFromRoleAndPrompt()
    {
        var provider = new FakeChatProvider(new[] { "ok" });
        var agent = new Agent(
            new AgentCard { Id = "writer", Model = "fast", Role = "You are a writer.", SystemPrompt = "Be concise." },
            provider, Spec());
        await agent.RunAsync("hello");
        var sent = provider.Calls[0].Messages;
        Assert.Equal("system", sent[0].Role);
        Assert.Equal("You are a writer.\n\nBe concise.", sent[0].Content);
        Assert.Equal("user", sent[1].Role);
        Assert.Equal("hello", sent[1].Content);
    }

    [Fact]
    public async Task Run_PrependsContextToUserMessage()
    {
        var provider = new FakeChatProvider(new[] { "ok" });
        var agent = new Agent(new AgentCard { Id = "writer", Model = "fast" }, provider, Spec());
        await agent.RunAsync("question", context: "RELEVANT CONTEXT");
        Assert.Equal("RELEVANT CONTEXT\n\nquestion", provider.Calls[0].Messages[^1].Content);
    }
}
