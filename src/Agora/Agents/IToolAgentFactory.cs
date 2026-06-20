using Agora.Configuration;
using Agora.HumanInTheLoop;
using Agora.Providers;
using Agora.Rag;
using Agora.Skills;

namespace Agora.Agents;

/// <summary>Everything a tool-capable agent backend needs to build an agent from config.</summary>
public sealed record AgentBuildContext
{
    public required AgentCard Card { get; init; }
    public required ModelSpec Spec { get; init; }
    public IReadOnlyList<Skill> Skills { get; init; } = Array.Empty<Skill>();
    public IReadOnlyList<string> Approvals { get; init; } = Array.Empty<string>();
    public IApprovalHandler? ApprovalHandler { get; init; }
    public IOutputInterpreter Interpreter { get; init; } = new SignalInterpreter();
    public McpConfig? Mcp { get; init; }

    /// <summary>Shared knowledge base read access for the <c>rag_search</c> tool.</summary>
    public RagPipeline? Rag { get; init; }

    /// <summary>Shared knowledge base write access for the <c>rag_write</c> tool.</summary>
    public KnowledgeBase? KnowledgeBase { get; init; }

    /// <summary>Callback for the <c>ask_agent</c> tool: <c>(targetAgentId, question) =&gt; answer</c>.
    /// Null in answer-mode sub-calls so an interrogated agent cannot ask back (no recursion).</summary>
    public Func<string, string, Task<string>>? AskAgent { get; init; }
}

/// <summary>
/// Builds a tool-capable IAgent (skills + MCP/function tools). Implemented by the framework layer
/// (Agora.AgentFramework) so the core stays framework-free; tests inject a fake.
/// </summary>
public interface IToolAgentFactory
{
    /// <summary>Builds a tool-capable agent from the supplied build context.</summary>
    IAgent Create(AgentBuildContext context);
}
