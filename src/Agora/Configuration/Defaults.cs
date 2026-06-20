namespace Agora.Configuration;

/// <summary>Default model and runtime settings applied to agents that don't override them.</summary>
public sealed class Defaults
{
    /// <summary>Default model alias used by agents without an explicit <c>model</c>.</summary>
    public string? Model { get; set; }

    /// <summary>Sampling temperature.</summary>
    public double Temperature { get; set; } = 0.2;

    /// <summary>Maximum tokens to generate per completion.</summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>Completion timeout in seconds.</summary>
    public double Timeout { get; set; } = 120;

    /// <summary>Number of retry attempts on transient provider failures.</summary>
    public int Retries { get; set; } = 2;

    /// <summary>Base delay (seconds) for exponential retry backoff.</summary>
    public double RetryBaseDelay { get; set; } = 0.5;
}
