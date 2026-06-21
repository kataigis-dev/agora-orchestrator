using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
namespace Agora.Providers.Contracts;

/// <summary>Abstraction over an LLM chat backend; the core depends only on this interface.</summary>
public interface IChatProvider
{
    /// <summary>Completes the message list using the given model spec and returns the result.</summary>
    Task<CompletionResult> CompleteAsync(
        IReadOnlyList<ChatMessage> messages, ModelSpec spec, CancellationToken cancellationToken = default);
}
