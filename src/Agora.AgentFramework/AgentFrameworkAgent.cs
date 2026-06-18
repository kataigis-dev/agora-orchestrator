using Agora.Agents;
using Agora.HumanInTheLoop;
using Agora.Resilience;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework;

/// <summary>
/// A tool-capable <see cref="IAgent"/> backed by Microsoft Agent Framework. Tools are the agent's
/// skills (a <c>load_skill</c> function) plus allow-listed MCP tools; tools named in the agent's
/// approvals are wrapped in <see cref="ApprovalRequiredAIFunction"/> and gated through the injected
/// <see cref="IApprovalHandler"/>. Model calls go through the shared <see cref="RetryPolicy"/>.
/// </summary>
public sealed class AgentFrameworkAgent : IAgent
{
    private readonly AgentBuildContext _ctx;
    private readonly RetryPolicy _retry = new(new SystemClock());

    public AgentFrameworkAgent(AgentBuildContext ctx) => _ctx = ctx;

    public async Task<AgentResult> RunAsync(string userInput, string context = "")
    {
        var spec = _ctx.Spec;
        var approvals = _ctx.Approvals.ToHashSet(StringComparer.Ordinal);

        var tools = new List<AITool>();
        if (_ctx.Skills.Count > 0)
            tools.Add(SkillTools.LoadSkill(_ctx.Skills));

        await using var mcp = await McpToolSession.ConnectAsync(_ctx.Mcp, _ctx.Card.Tools, CancellationToken.None);
        foreach (var tool in mcp.Tools)
        {
            if (!approvals.Contains(tool.Name))
            {
                tools.Add(tool);
            }
            else if (tool is AIFunction fn)
            {
                tools.Add(new ApprovalRequiredAIFunction(fn));
            }
            else
            {
                throw new InvalidOperationException(
                    $"tool '{tool.Name}' requires approval but is not an AIFunction that can be gated");
            }
        }

        var chatClient = ChatClients.Build(spec);
        AIAgent agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Temperature = (float)spec.Temperature,
                MaxOutputTokens = spec.MaxTokens,
                Tools = tools.Count > 0 ? tools : null,
            },
        });

        var messages = new List<Microsoft.Extensions.AI.ChatMessage>();
        var instructions = BuildInstructions();
        if (!string.IsNullOrEmpty(instructions))
            messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, instructions));
        var userContent = string.IsNullOrEmpty(context) ? userInput : $"{context}\n\n{userInput}";
        messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userContent));

        const int maxApprovalRounds = 10;
        var response = await _retry.ExecuteAsync(ct => agent.RunAsync(messages, cancellationToken: ct), spec);

        // Human-in-the-loop: resolve any tool-approval requests, then re-run until none remain.
        for (var round = 0; ; round++)
        {
            var requests = response.Messages
                .SelectMany(m => m.Contents)
                .OfType<ToolApprovalRequestContent>()
                .ToList();
            if (requests.Count == 0)
                break;
            if (round >= maxApprovalRounds)
                throw new InvalidOperationException(
                    $"agent '{_ctx.Card.Id}' exceeded {maxApprovalRounds} tool-approval rounds");

            foreach (var request in requests)
            {
                var approved = await ApprovalFor(request);
                messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, new List<AIContent> { request }));
                messages.Add(new Microsoft.Extensions.AI.ChatMessage(
                    ChatRole.User,
                    new List<AIContent> { request.CreateResponse(approved, approved ? "approved" : "rejected") }));
            }

            response = await _retry.ExecuteAsync(ct => agent.RunAsync(messages, cancellationToken: ct), spec);
        }

        var (output, signals, artifacts) = _ctx.Interpreter.Interpret(response.Text ?? string.Empty);
        return new AgentResult { Output = output, Signals = signals, Artifacts = artifacts };
    }

    private async Task<bool> ApprovalFor(ToolApprovalRequestContent request)
    {
        if (_ctx.ApprovalHandler is null)
            return false; // fail-closed; Runtime already rejects this config, this is belt-and-braces

        var (name, args) = Describe(request.ToolCall);
        return await _ctx.ApprovalHandler.RequestAsync(new ApprovalRequest
        {
            AgentId = _ctx.Card.Id,
            FunctionName = name,
            Arguments = args,
        });
    }

    private static (string Name, string Arguments) Describe(ToolCallContent call) => call switch
    {
        FunctionCallContent f => (f.Name, f.Arguments is null
            ? string.Empty
            : string.Join(", ", f.Arguments.Select(kv => $"{kv.Key}={kv.Value}"))),
        McpServerToolCallContent m => (m.Name, m.Arguments?.ToString() ?? string.Empty),
        _ => (call.CallId, string.Empty),
    };

    private string BuildInstructions()
    {
        var parts = new[] { _ctx.Card.Role, _ctx.Card.SystemPrompt }.Where(p => !string.IsNullOrEmpty(p));
        return string.Join("\n\n", parts);
    }
}
