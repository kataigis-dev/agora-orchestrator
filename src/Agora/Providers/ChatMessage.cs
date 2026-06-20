namespace Agora.Providers;

/// <summary>One chat message in a completion request.</summary>
/// <param name="Role">Message role (<c>system</c>, <c>user</c>, or <c>assistant</c>).</param>
/// <param name="Content">Message text.</param>
public sealed record ChatMessage(string Role, string Content);
