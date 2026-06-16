namespace Agora.Configuration;

public sealed class AgoraConfig
{
    public string Version { get; set; } = "1";
    public string Communication { get; set; } = "h2c";
    public Defaults Defaults { get; set; } = new();
    public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
    public Dictionary<string, ModelConfig> Models { get; set; } = new();
    public Dictionary<string, AgentConfig> Agents { get; set; } = new();
    public GraphConfig? Graph { get; set; }
    public RagConfig? Rag { get; set; }
    public SkillsConfig? Skills { get; set; }
    public McpConfig? Mcp { get; set; }
}
