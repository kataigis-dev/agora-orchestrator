namespace Agora.Providers;

public sealed record ModelSpec
{
    public required string Alias { get; init; }
    public required string Provider { get; init; }
    public required string Model { get; init; }
    public double Temperature { get; init; }
    public int MaxTokens { get; init; }
    public double Timeout { get; init; }
    public int Retries { get; init; }
    public double RetryBaseDelay { get; init; }
    public string? ApiKey { get; init; }
    public string? ApiBase { get; init; }
}
