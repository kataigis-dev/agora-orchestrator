using Agora.AgentFramework.Tools;
using Agora.AgentFramework.Mcp;
using Agora.AgentFramework.Specs;
using Agora.AgentFramework.Rag;
using Agora.AgentFramework.Providers;
using Agora.AgentFramework.Agents;
using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using Agora.HumanInTheLoop;
using Agora.Resilience;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework.Agents;

/// <summary>
/// A tool-capable <see cref="IAgent"/> backed by Microsoft Agent Framework. Tools are the agent's
/// skills (a <c>load_skill</c> function) plus allow-listed MCP tools; tools named in the agent's
/// approvals are wrapped in <see cref="ApprovalRequiredAIFunction"/> and gated through the injected
/// <see cref="IApprovalHandler"/>. Model calls go through the shared <see cref="RetryPolicy"/>.
/// </summary>
public sealed class AgentFrameworkAgent : IAgent
{
    private readonly AgentBuildContext _ctx;
    private readonly ChatClientCache _clients;
    private readonly RetryPolicy _retry = new(new SystemClock());

    /// <summary>Creates the agent from its build context (card, model, skills, tools, handlers) and the
    /// shared client cache used to reuse the underlying chat client across runs.</summary>
    public AgentFrameworkAgent(AgentBuildContext ctx, ChatClientCache clients)
    {
        _ctx = ctx;
        _clients = clients;
    }

    /// <summary>Assembles the tool set (skills, filesystem, RAG, ask_agent, MCP), runs the agent
    /// through the approval loop until no approvals remain, and interprets the final reply.
    /// <paramref name="onChunk"/> is ignored: tool agents don't stream yet.</summary>
    public async Task<AgentResult> RunAsync(string userInput, string context = "", Action<string>? onChunk = null)
    {
        // Streaming is not wired through the tool-calling/approval loop yet; tool agents ignore onChunk.
        var spec = _ctx.Spec;
        var approvals = _ctx.Approvals.ToHashSet(StringComparer.Ordinal);

        // Assemble every candidate tool unwrapped: skills, filesystem, shared KB, ask_agent, MCP.
        var rawTools = new List<AITool>();
        if (_ctx.Skills.Count > 0)
            rawTools.Add(SkillTools.LoadSkill(_ctx.Skills));
        rawTools.AddRange(BuiltInFileTools.Create(_ctx.FilesystemRoot, _ctx.Card.Tools));
        rawTools.AddRange(RagTools.Create(_ctx.Card.Tools, _ctx.Rag, _ctx.KnowledgeBase, _ctx.Card.Id));
        rawTools.AddRange(SpecTools.Create(_ctx.Card.Tools, _ctx.SpecStore, _ctx.SpecRequireCriteria));
        rawTools.AddRange(CheckTools.Create(_ctx.Card.Tools, _ctx.CheckRunner, _ctx.SpecStore, _ctx.SpecRequireCriteria));
        if (AskAgentTool.Create(_ctx.Card.Tools, _ctx.AskAgent) is { } askAgent)
            rawTools.Add(askAgent);

        await using var mcp = await McpToolSession.ConnectAsync(_ctx.Mcp, _ctx.Card.Tools, CancellationToken.None);
        rawTools.AddRange(mcp.Tools);

        // Gate any approval-listed tool — built-in or MCP — through ApprovalRequiredAIFunction.
        var tools = new List<AITool>(rawTools.Count);
        foreach (var tool in rawTools)
        {
            if (!approvals.Contains(tool.Name))
                tools.Add(tool);
            else if (tool is AIFunction fn)
                tools.Add(new ApprovalRequiredAIFunction(fn));
            else
                throw new InvalidOperationException(
                    $"tool '{tool.Name}' requires approval but is not an AIFunction that can be gated");
        }

        var chatClient = _clients.Get(spec);
        AIAgent agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Temperature = (float)spec.Temperature,
                MaxOutputTokens = spec.MaxTokens,
                Tools = tools.Count > 0 ? tools : null,
            },
        });

        var cacheAdapter = CacheAdapters.For(spec);
        var messages = new List<Microsoft.Extensions.AI.ChatMessage>();
        var instructions = _ctx.Card.ComposeInstructions();
        if (!string.IsNullOrEmpty(instructions))
        {
            // The instructions are stable across the run → mark them as a cacheable prefix.
            var system = new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, instructions);
            cacheAdapter.Mark(system);
            messages.Add(system);
        }
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
        var (input, generated, cacheRead, cacheWrite) = SumUsage(response.Messages, cacheAdapter);
        return new AgentResult
        {
            Output = output,
            InputTokens = input,
            OutputTokens = generated,
            CacheReadTokens = cacheRead,
            CacheWriteTokens = cacheWrite,
            Signals = signals,
            Artifacts = artifacts,
        };
    }

    /// <summary>Sums per-message usage (the tool path reports usage per message rather than as one total)
    /// through the provider's cache adapter, so cache reads/writes are mapped consistently.</summary>
    private static (int Input, int Output, int CacheRead, int CacheWrite) SumUsage(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ICacheAdapter adapter)
    {
        int input = 0, output = 0, cacheRead = 0, cacheWrite = 0;
        foreach (var content in messages.SelectMany(m => m.Contents).OfType<UsageContent>())
        {
            var (i, o, r, w) = adapter.MapUsage(content.Details);
            input += i; output += o; cacheRead += r; cacheWrite += w;
        }
        return (input, output, cacheRead, cacheWrite);
    }

    /// <summary>Routes a tool-approval request to the injected handler (fail-closed if none).</summary>
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

    /// <summary>Extracts a tool call's name and a readable argument string for the approval prompt.</summary>
    private static (string Name, string Arguments) Describe(ToolCallContent call) => call switch
    {
        FunctionCallContent f => (f.Name, f.Arguments is null
            ? string.Empty
            : string.Join(", ", f.Arguments.Select(kv => $"{kv.Key}={kv.Value}"))),
        McpServerToolCallContent m => (m.Name, m.Arguments?.ToString() ?? string.Empty),
        _ => (call.CallId, string.Empty),
    };
}
