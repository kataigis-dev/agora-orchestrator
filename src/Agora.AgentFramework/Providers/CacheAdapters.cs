using Agora.Providers.Models;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework.Providers;

/// <summary>Per-provider prompt-caching behaviour behind one seam: how a provider's
/// <see cref="ChatMessage.CacheStable"/> hint is marked on the wire, and how its response usage maps onto
/// Agora's token counts (including cache reads/writes). One adapter per caching mechanism — resolved by
/// <see cref="CacheAdapters.For"/> — so adding a provider's caching means adding an adapter, not editing
/// the provider stack.</summary>
internal interface ICacheAdapter
{
    /// <summary>Marks a cache-stable message for the provider's mechanism (a no-op where caching is
    /// implicit). Only called for messages the caller flagged <see cref="ChatMessage.CacheStable"/>.</summary>
    void Mark(Microsoft.Extensions.AI.ChatMessage message);

    /// <summary>Maps a usage record onto Agora's token counts (all zero when <paramref name="usage"/> is
    /// null). Cache-read stays a subset of input.</summary>
    (int Input, int Output, int CacheRead, int CacheWrite) MapUsage(UsageDetails? usage);
}

/// <summary>Caching for providers that cache a stable prefix automatically (OpenAI, Ollama, Copilot, and
/// unknown OpenAI-compatible endpoints): no wire marker, and no separate cache-write count.</summary>
internal sealed class ImplicitCacheAdapter : ICacheAdapter
{
    /// <inheritdoc />
    public void Mark(Microsoft.Extensions.AI.ChatMessage message) { /* implicit: stable prefix reused with no marker */ }

    /// <inheritdoc />
    public (int Input, int Output, int CacheRead, int CacheWrite) MapUsage(UsageDetails? usage)
    {
        if (usage is null)
            return (0, 0, 0, 0);
        // CachedInputTokenCount is the normalized cache-read count (OpenAI prompt_tokens_details.cached_tokens).
        return ((int)(usage.InputTokenCount ?? 0), (int)(usage.OutputTokenCount ?? 0),
            (int)(usage.CachedInputTokenCount ?? 0), 0);
    }
}

/// <summary>Caching for Anthropic (breakpoint mechanism): tags <c>cache_control</c> on cache-stable
/// blocks, and reads the <c>cache_creation_input_tokens</c> write count that
/// <see cref="AnthropicChatClient"/> surfaces via <see cref="UsageDetails.AdditionalCounts"/>.</summary>
internal sealed class AnthropicCacheAdapter : ICacheAdapter
{
    // Provider-specific keys for cache-creation (write) tokens, surfaced via AdditionalCounts.
    private static readonly string[] CacheWriteKeys = { "cache_creation_input_tokens", "CacheCreationInputTokens" };

    /// <inheritdoc />
    public void Mark(Microsoft.Extensions.AI.ChatMessage message)
        => message.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            ["cache_control"] = new Dictionary<string, object> { ["type"] = "ephemeral" },
        };

    /// <inheritdoc />
    public (int Input, int Output, int CacheRead, int CacheWrite) MapUsage(UsageDetails? usage)
    {
        if (usage is null)
            return (0, 0, 0, 0);
        var cacheWrite = 0;
        if (usage.AdditionalCounts is { } extra)
            foreach (var key in CacheWriteKeys)
                if (extra.TryGetValue(key, out var value)) { cacheWrite = (int)value; break; }
        return ((int)(usage.InputTokenCount ?? 0), (int)(usage.OutputTokenCount ?? 0),
            (int)(usage.CachedInputTokenCount ?? 0), cacheWrite);
    }
}

/// <summary>Resolves the <see cref="ICacheAdapter"/> for a model spec — the single place mapping a
/// provider to its prompt-caching behaviour. Adapters are stateless, so shared instances are reused.</summary>
internal static class CacheAdapters
{
    private static readonly ICacheAdapter Anthropic = new AnthropicCacheAdapter();
    private static readonly ICacheAdapter Implicit = new ImplicitCacheAdapter();

    /// <summary>Returns the caching adapter for the spec's provider (case-insensitive). Unknown providers
    /// default to implicit caching (most OpenAI-compatible endpoints reuse a stable prefix automatically).</summary>
    public static ICacheAdapter For(ModelSpec spec) => spec.Provider?.ToLowerInvariant() switch
    {
        "anthropic" or "claude" => Anthropic,
        _ => Implicit,
    };
}
