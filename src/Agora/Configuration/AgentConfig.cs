namespace Agora.Configuration;

public sealed class AgentConfig
{
    public string? Model { get; set; }
    public string Role { get; set; } = "";
    public string? SystemPrompt { get; set; }
    public string? SystemPromptFile { get; set; }
    public double? Timeout { get; set; }
    public List<string> Skills { get; set; } = new();
    public List<string> Tools { get; set; } = new();
    public List<string> Approvals { get; set; } = new();
}
