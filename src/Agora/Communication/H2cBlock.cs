namespace Agora.Communication;

/// <summary>A single H2C block: <c>[Type:Subtype]</c> with key:value fields.</summary>
public sealed record H2cBlock
{
    /// <summary>Block type (e.g. <c>ARCH</c>, <c>STATE</c>, <c>TEST</c>).</summary>
    public required string Type { get; init; }

    /// <summary>Block subtype (e.g. <c>PLAN</c>, <c>DONE</c>, <c>PASS</c>); doubles as a signal name.</summary>
    public required string Subtype { get; init; }

    /// <summary>Optional <c>key:value</c> fields carried by the block.</summary>
    public IReadOnlyDictionary<string, string> Fields { get; init; } = new Dictionary<string, string>();
}
