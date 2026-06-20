namespace Agora.Rag;

/// <summary>Resolved embedder request handed to an edge-injected embedder resolver:
/// the backend <see cref="Type"/> plus the provider's model and resolved credentials.</summary>
public sealed record EmbedderSpec(string Type, string Provider, string Model, string? ApiKey, string? ApiBase);
