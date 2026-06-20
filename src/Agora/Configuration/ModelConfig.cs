namespace Agora.Configuration;

/// <summary>Maps a model alias to a provider and a concrete model name.</summary>
public sealed class ModelConfig
{
    /// <summary>Name of the provider that serves this model.</summary>
    public string Provider { get; set; } = "";

    /// <summary>Concrete model identifier passed to the provider (e.g. <c>gpt-4o</c>).</summary>
    public string Model { get; set; } = "";
}
