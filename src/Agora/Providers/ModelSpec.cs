namespace Agora.Providers;

/// <summary>Fully-resolved model parameters and credentials used to make a completion call.</summary>
public sealed record ModelSpec
{
    /// <summary>Config alias this spec was resolved from.</summary>
    public required string Alias { get; init; }

    /// <summary>Provider name serving the model.</summary>
    public required string Provider { get; init; }

    /// <summary>Concrete model identifier.</summary>
    public required string Model { get; init; }

    /// <summary>Sampling temperature.</summary>
    public double Temperature { get; init; }

    /// <summary>Maximum tokens to generate.</summary>
    public int MaxTokens { get; init; }

    /// <summary>Completion timeout in seconds.</summary>
    public double Timeout { get; init; }

    /// <summary>Retry attempts on transient failures.</summary>
    public int Retries { get; init; }

    /// <summary>Base delay (seconds) for exponential retry backoff.</summary>
    public double RetryBaseDelay { get; init; }

    /// <summary>Resolved API key, if any.</summary>
    public string? ApiKey { get; init; }

    /// <summary>Provider base URL, if any.</summary>
    public string? ApiBase { get; init; }
}
