namespace Agora.Configuration;

public sealed class McpServerConfig
{
    public string? Command { get; set; }
    public List<string> Args { get; set; } = new();
    public string? Url { get; set; }
}

public sealed class McpConfig
{
    public Dictionary<string, McpServerConfig> Servers { get; set; } = new();
}
