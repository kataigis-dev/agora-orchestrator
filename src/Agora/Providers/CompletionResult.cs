namespace Agora.Providers;

/// <summary>Result of a chat completion: generated text plus token usage.</summary>
public sealed record CompletionResult
{
    /// <summary>The generated completion text.</summary>
    public required string Text { get; init; }

    /// <summary>Prompt tokens consumed.</summary>
    public int InputTokens { get; init; }

    /// <summary>Tokens generated.</summary>
    public int OutputTokens { get; init; }

    /// <summary>Concrete model that produced the completion.</summary>
    public string Model { get; init; } = "";
}
