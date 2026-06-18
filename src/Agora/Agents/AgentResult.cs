namespace Agora.Agents;

public sealed record AgentResult
{
    public required string Output { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public Dictionary<string, object> Signals { get; init; } = new();
    public Dictionary<string, string> Artifacts { get; init; } = new();
}
