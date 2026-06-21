using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
namespace Agora.Providers.Models;

/// <summary>Result of a chat completion: generated text plus token usage (including prompt-cache hits).</summary>
public sealed record CompletionResult
{
    /// <summary>The generated completion text.</summary>
    public required string Text { get; init; }

    /// <summary>Prompt tokens consumed (cached and uncached).</summary>
    public int InputTokens { get; init; }

    /// <summary>Tokens generated.</summary>
    public int OutputTokens { get; init; }

    /// <summary>Prompt tokens served from the provider's cache (a subset of <see cref="InputTokens"/>);
    /// the lever measured by prompt caching. Zero when caching is off or unsupported.</summary>
    public int CacheReadTokens { get; init; }

    /// <summary>Prompt tokens written to the cache on this call (cache creation); provider-specific,
    /// zero when not reported.</summary>
    public int CacheWriteTokens { get; init; }

    /// <summary>Concrete model that produced the completion.</summary>
    public string Model { get; init; } = "";
}
