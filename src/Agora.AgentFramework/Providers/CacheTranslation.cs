using Agora.AgentFramework.Tools;
using Agora.AgentFramework.Mcp;
using Agora.AgentFramework.Specs;
using Agora.AgentFramework.Rag;
using Agora.AgentFramework.Providers;
using Agora.AgentFramework.Agents;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework.Providers;

/// <summary>Translates Agora's provider-agnostic <see cref="ChatMessage.CacheStable"/> hint into a
/// provider's native prompt-caching mechanism on a Microsoft.Extensions.AI message. Breakpoint
/// providers (Anthropic) get an explicit <c>cache_control</c> marker; implicit providers cache a
/// stable prefix automatically, so the marker is a no-op there.</summary>
internal static class CacheTranslation
{
    /// <summary>Adds a cache breakpoint to <paramref name="message"/> when the spec's provider needs
    /// explicit markers; otherwise leaves it untouched.</summary>
    public static void MarkStable(Microsoft.Extensions.AI.ChatMessage message, ModelSpec spec)
    {
        if (PromptCaching.For(spec.Provider) != CachingMode.Breakpoint)
            return;
        message.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            ["cache_control"] = new Dictionary<string, object> { ["type"] = "ephemeral" },
        };
    }
}
