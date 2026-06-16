namespace Agora.Providers;

public sealed record CompletionResult
{
    public required string Text { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public string Model { get; init; } = "";
}
