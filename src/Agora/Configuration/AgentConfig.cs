namespace Agora.Configuration;

/// <summary>YAML configuration for a single agent under the <c>agents</c> section.</summary>
public sealed class AgentConfig
{
    /// <summary>Model alias to use; falls back to <c>defaults.model</c> when null.</summary>
    public string? Model { get; set; }

    /// <summary>Short role descriptor prepended to the system prompt.</summary>
    public string Role { get; set; } = "";

    /// <summary>Inline system prompt; mutually exclusive with <see cref="SystemPromptFile"/>.</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>Path to a file whose contents become the system prompt.</summary>
    public string? SystemPromptFile { get; set; }

    /// <summary>Per-agent completion timeout in seconds; overrides the default when set.</summary>
    public double? Timeout { get; set; }

    /// <summary>Skill names available to the agent.</summary>
    public List<string> Skills { get; set; } = new();

    /// <summary>Allow-list of tool names the agent may call.</summary>
    public List<string> Tools { get; set; } = new();

    /// <summary>Subset of <see cref="Tools"/> requiring human approval before execution.</summary>
    public List<string> Approvals { get; set; } = new();
}
