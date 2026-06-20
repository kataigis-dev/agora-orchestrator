namespace Agora.Configuration;

/// <summary>Configuration for one MCP tool server (stdio via <see cref="Command"/>/<see cref="Args"/>,
/// or HTTP via <see cref="Url"/>).</summary>
public sealed class McpServerConfig
{
    /// <summary>Executable to launch for an stdio server.</summary>
    public string? Command { get; set; }

    /// <summary>Arguments passed to <see cref="Command"/>.</summary>
    public List<string> Args { get; set; } = new();

    /// <summary>Endpoint URL for an HTTP server.</summary>
    public string? Url { get; set; }
}

/// <summary>MCP tool-server configuration.</summary>
public sealed class McpConfig
{
    /// <summary>MCP servers keyed by name.</summary>
    public Dictionary<string, McpServerConfig> Servers { get; set; } = new();
}
