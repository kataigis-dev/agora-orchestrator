namespace Agora.Communication;

/// <summary>A single H2C block: <c>[Type:Subtype]</c> with key:value fields.</summary>
public sealed record H2cBlock
{
    public required string Type { get; init; }
    public required string Subtype { get; init; }
    public IReadOnlyDictionary<string, string> Fields { get; init; } = new Dictionary<string, string>();
}
