namespace Agora.Providers;

/// <summary>How a provider exposes prompt caching. The mechanisms are heterogeneous, so a single
/// "enable cache" switch would be the wrong abstraction: callers mark stable content with
/// <see cref="ChatMessage.CacheStable"/>, and each provider adapter translates that intent into its
/// native mechanism (or treats it as a no-op).</summary>
public enum CachingMode
{
    /// <summary>No prompt caching available; the marker is ignored.</summary>
    None,

    /// <summary>Caching is automatic on a stable prefix with no API call (OpenAI, most
    /// OpenAI-compatible endpoints, llama.cpp/Ollama KV reuse). The marker is a no-op; the real
    /// lever is keeping stable content first and byte-identical across calls.</summary>
    Implicit,

    /// <summary>Caching requires explicit per-block breakpoints (Anthropic <c>cache_control</c>).
    /// The adapter tags <see cref="ChatMessage.CacheStable"/> messages.</summary>
    Breakpoint,

    /// <summary>Caching requires managing an explicit cache resource with its own lifecycle
    /// (Google Gemini <c>CachedContent</c>). The adapter creates/reuses a handle for the stable
    /// segment.</summary>
    Resource,
}

/// <summary>Maps a provider to its prompt-caching mechanism so the runtime knows what to expect and
/// adapters know how to translate <see cref="ChatMessage.CacheStable"/>.</summary>
public static class PromptCaching
{
    /// <summary>Returns the caching mechanism for a provider name (case-insensitive). Unknown
    /// providers default to <see cref="CachingMode.Implicit"/>: the translation is a no-op and most
    /// OpenAI-compatible endpoints cache a stable prefix automatically.</summary>
    public static CachingMode For(string provider) => provider?.ToLowerInvariant() switch
    {
        "anthropic" or "claude" => CachingMode.Breakpoint,
        "google" or "gemini" or "vertex" => CachingMode.Resource,
        "openai" or "azure-openai" or "ollama" or "github-copilot" or "copilot" => CachingMode.Implicit,
        null => CachingMode.None,
        _ => CachingMode.Implicit,
    };
}
