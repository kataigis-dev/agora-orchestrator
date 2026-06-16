namespace Agora.Configuration;

public sealed class Defaults
{
    public string? Model { get; set; }
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 4096;
    public double Timeout { get; set; } = 120;
    public int Retries { get; set; } = 2;
    public double RetryBaseDelay { get; set; } = 0.5;
}
