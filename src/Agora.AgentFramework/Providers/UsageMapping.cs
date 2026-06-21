using Agora.AgentFramework.Tools;
using Agora.AgentFramework.Mcp;
using Agora.AgentFramework.Specs;
using Agora.AgentFramework.Rag;
using Agora.AgentFramework.Providers;
using Agora.AgentFramework.Agents;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework.Providers;

/// <summary>Maps Microsoft.Extensions.AI <see cref="UsageDetails"/> onto Agora's token counts,
/// including prompt-cache reads/writes where the provider reports them.</summary>
internal static class UsageMapping
{
    // Provider-specific keys for cache-creation (write) tokens, which MEAI surfaces via AdditionalCounts.
    private static readonly string[] CacheWriteKeys =
    {
        "cache_creation_input_tokens", // Anthropic
        "CacheCreationInputTokens",
    };

    /// <summary>Reads input/output and cache read/write token counts from a usage record (all zero
    /// when <paramref name="usage"/> is null).</summary>
    public static (int Input, int Output, int CacheRead, int CacheWrite) From(UsageDetails? usage)
    {
        if (usage is null)
            return (0, 0, 0, 0);
        var input = (int)(usage.InputTokenCount ?? 0);
        var output = (int)(usage.OutputTokenCount ?? 0);
        // CachedInputTokenCount is the normalized cache-read count (Anthropic cache_read_input_tokens,
        // OpenAI prompt_tokens_details.cached_tokens).
        var cacheRead = (int)(usage.CachedInputTokenCount ?? 0);
        var cacheWrite = 0;
        if (usage.AdditionalCounts is { } extra)
            foreach (var key in CacheWriteKeys)
                if (extra.TryGetValue(key, out var value)) { cacheWrite = (int)value; break; }
        return (input, output, cacheRead, cacheWrite);
    }

    /// <summary>Aggregates usage carried as <see cref="UsageContent"/> across a response's messages
    /// (used on the tool/agent path, which reports usage per message rather than as one total).</summary>
    public static (int Input, int Output, int CacheRead, int CacheWrite) From(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages)
    {
        int input = 0, output = 0, cacheRead = 0, cacheWrite = 0;
        foreach (var content in messages.SelectMany(m => m.Contents).OfType<UsageContent>())
        {
            var (i, o, r, w) = From(content.Details);
            input += i; output += o; cacheRead += r; cacheWrite += w;
        }
        return (input, output, cacheRead, cacheWrite);
    }
}
