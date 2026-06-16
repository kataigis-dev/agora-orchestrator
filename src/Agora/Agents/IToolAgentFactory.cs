using Agora.Configuration;
using Agora.HumanInTheLoop;
using Agora.Providers;
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
}

/// <summary>
/// Builds a tool-capable IAgent (skills + MCP/function tools). Implemented by the framework layer
/// (Agora.AgentFramework) so the core stays framework-free; tests inject a fake.
/// </summary>
public interface IToolAgentFactory
{
    IAgent Create(AgentBuildContext context);
}
