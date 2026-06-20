namespace Agora.Configuration;

/// <summary>Configuration for an LLM provider.</summary>
public sealed class ProviderConfig
{
    /// <summary>Name of the environment variable holding the API key (null for keyless/local providers).</summary>
    public string? ApiKeyEnv { get; set; }

    /// <summary>Base URL for the provider's API; used for OpenAI-compatible/local endpoints.</summary>
    public string? BaseUrl { get; set; }
}
