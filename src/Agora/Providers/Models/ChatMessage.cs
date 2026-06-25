using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
namespace Agora.Providers.Models;

/// <summary>One chat message in a completion request.</summary>
/// <param name="Role">Message role (<c>system</c>, <c>user</c>, or <c>assistant</c>).</param>
/// <param name="Content">Message text.</param>
/// <param name="CacheStable">Provider-agnostic prompt-caching hint: <c>true</c> marks the content as
/// stable and reusable across calls (system prompts, role instructions, injected KB context), so a
/// cache-aware provider can reuse it as a prefix. Providers that cache implicitly (OpenAI, Ollama)
/// ignore the flag; breakpoint providers (Anthropic) translate it into a cache marker. Each provider's
/// caching is owned by its cache adapter in <c>Agora.AgentFramework.Providers</c>.</param>
public sealed record ChatMessage(string Role, string Content, bool CacheStable = false);
