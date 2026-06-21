using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
namespace Agora.Agents.Models;

/// <summary>Immutable identity and capability descriptor for a single agent.</summary>
public sealed record AgentCard
{
    /// <summary>Unique agent identifier (used as the graph node key).</summary>
    public required string Id { get; init; }

    /// <summary>Model alias resolved against the config's <c>models</c> section.</summary>
    public required string Model { get; init; }

    /// <summary>Short role descriptor prepended to the system prompt.</summary>
    public string Role { get; init; } = "";

    /// <summary>Full system prompt for the agent.</summary>
    public string SystemPrompt { get; init; } = "";

    /// <summary>Names of skills available to the agent.</summary>
    public IReadOnlyList<string> Skills { get; init; } = Array.Empty<string>();

    /// <summary>Allow-list of tool names the agent may call (filesystem, MCP, RAG, ask_agent).</summary>
    public IReadOnlyList<string> Tools { get; init; } = Array.Empty<string>();

    /// <summary>Subset of <see cref="Tools"/> that require human approval before running.</summary>
    public IReadOnlyList<string> Approvals { get; init; } = Array.Empty<string>();

    /// <summary>Composes the agent's instructions by joining the non-empty <see cref="Role"/> and
    /// <see cref="SystemPrompt"/> with a blank line (used as the system message / agent instructions).</summary>
    public string ComposeInstructions() =>
        string.Join("\n\n", new[] { Role, SystemPrompt }.Where(p => !string.IsNullOrEmpty(p)));
}
