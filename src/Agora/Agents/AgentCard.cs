namespace Agora.Agents;

public sealed record AgentCard
{
    public required string Id { get; init; }
    public required string Model { get; init; }
    public string Role { get; init; } = "";
    public string SystemPrompt { get; init; } = "";
    public IReadOnlyList<string> Skills { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Tools { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Approvals { get; init; } = Array.Empty<string>();
}
