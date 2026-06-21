namespace Agora.Configuration;

/// <summary>Root of the YAML configuration: providers, models, agents, the graph, and optional
/// RAG/skills/MCP/memory sections.</summary>
public sealed class AgoraConfig
{
    /// <summary>Config schema version.</summary>
    public string Version { get; set; } = "1";

    /// <summary>Communication mode: <c>h2c</c> (token-compressed blocks) or <c>natural</c>.</summary>
    public string Communication { get; set; } = "h2c";

    /// <summary>When true, a graph hop passes only the sending agent's explicit
    /// <c>handoff</c> artifact to the next agent instead of its full output.</summary>
    public bool? Handoff { get; set; }

    /// <summary>Language agents must use when generating documents/responses (e.g. "English").
    /// Injected into every agent's system prompt when set.</summary>
    public string? Language { get; set; }

    /// <summary>Default model alias and runtime settings applied when an agent omits its own.</summary>
    public Defaults Defaults { get; set; } = new();

    /// <summary>LLM providers keyed by name (e.g. <c>openai</c>, <c>ollama</c>, <c>github-copilot</c>).</summary>
    public Dictionary<string, ProviderConfig> Providers { get; set; } = new();

    /// <summary>Model aliases keyed by name, each mapping to a provider + concrete model.</summary>
    public Dictionary<string, ModelConfig> Models { get; set; } = new();

    /// <summary>Agents keyed by id.</summary>
    public Dictionary<string, AgentConfig> Agents { get; set; } = new();

    /// <summary>Optional execution graph; required for multi-agent runs.</summary>
    public GraphConfig? Graph { get; set; }

    /// <summary>Optional RAG / shared knowledge-base configuration.</summary>
    public RagConfig? Rag { get; set; }

    /// <summary>Optional skills configuration (directories of SKILL.md files).</summary>
    public SkillsConfig? Skills { get; set; }

    /// <summary>Optional MCP tool-server configuration.</summary>
    public McpConfig? Mcp { get; set; }

    /// <summary>Optional RAG-backed context-memory configuration.</summary>
    public MemoryConfig? Memory { get; set; }

    /// <summary>Optional structured spec-driven-development configuration.</summary>
    public SpecConfig? Spec { get; set; }

    /// <summary>Optional real build/test execution (allow-listed checks) configuration.</summary>
    public ChecksConfig? Checks { get; set; }
}
