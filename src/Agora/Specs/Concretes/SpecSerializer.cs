using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Specs.Concretes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agora.Specs.Concretes;

/// <summary>Canonical JSON (de)serialization for a <see cref="SpecDocument"/>, shared by every
/// store implementation so the on-disk and over-MCP representations are identical. Enums are written
/// by name and output is indented so the artifact stays human-readable and diff-friendly.</summary>
public static class SpecSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Serializes a specification to canonical JSON.</summary>
    public static string Serialize(SpecDocument document) => JsonSerializer.Serialize(document, Options);

    /// <summary>Deserializes a specification from JSON, returning <see cref="SpecDocument.Empty"/> for
    /// blank input.</summary>
    public static SpecDocument Deserialize(string json)
        => string.IsNullOrWhiteSpace(json)
            ? SpecDocument.Empty
            : JsonSerializer.Deserialize<SpecDocument>(json, Options) ?? SpecDocument.Empty;
}
