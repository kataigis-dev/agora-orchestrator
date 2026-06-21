using System.Text.Json;
using Agora.AgentFramework.Agents;
using Agora.AgentFramework.Providers;
using Agora.AgentFramework.Rag;
using Agora.AgentFramework.Specs;
using Agora.AgentFramework.Mcp;
using Agora.AgentFramework.Tools;
using Microsoft.Extensions.AI;

namespace Agora.Tests.AgentFramework;

public class AskAgentToolTests
{
    private static AIFunctionArguments Args(params (string Key, object? Value)[] items)
    {
        var args = new AIFunctionArguments();
        foreach (var (k, v) in items) args[k] = v;
        return args;
    }

    private static string AsString(object? result) =>
        result is JsonElement je ? je.GetString() ?? "" : result?.ToString() ?? "";

    [Fact]
    public async Task AskAgent_InvokesCallback_WithTargetAndQuestion()
    {
        (string Target, string Question)? captured = null;
        Func<string, string, Task<string>> ask = (t, q) =>
        {
            captured = (t, q);
            return Task.FromResult("A SAYS HI");
        };
        var tool = (AIFunction)AskAgentTool.Create(new[] { "ask_agent" }, ask)!;

        var result = AsString(await tool.InvokeAsync(Args(("target", "planner"), ("question", "what is X"))));

        Assert.Equal("A SAYS HI", result);
        Assert.Equal(("planner", "what is X"), captured);
    }

    [Fact]
    public void NotCreated_WhenNotAllowListed()
        => Assert.Null(AskAgentTool.Create(Array.Empty<string>(), (_, _) => Task.FromResult("x")));

    [Fact]
    public void NotCreated_WhenNoCallback()
        => Assert.Null(AskAgentTool.Create(new[] { "ask_agent" }, ask: null));
}
