using System.Text.Json;

namespace Agora.Eval.Quality;

/// <summary>Loads quality scenarios from a single JSON file (one object or an array) or from a directory of
/// <c>*.quality.json</c> files.</summary>
public static class QualitySuiteLoader
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Loads scenarios from <paramref name="path"/> (a file or a directory).</summary>
    public static IReadOnlyList<QualityScenario> Load(string path)
    {
        if (Directory.Exists(path))
            return Directory.GetFiles(path, "*.quality.json")
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => ParseOne(File.ReadAllText(p), p))
                .ToList();

        if (!File.Exists(path))
            throw new FileNotFoundException($"suite path not found: {path}");

        var text = File.ReadAllText(path);
        if (text.TrimStart().StartsWith('['))
            return JsonSerializer.Deserialize<List<QualityScenario>>(text, Options)
                ?? throw new InvalidDataException($"empty or invalid suite file: {path}");
        return new List<QualityScenario> { ParseOne(text, path) };
    }

    private static QualityScenario ParseOne(string text, string path)
        => JsonSerializer.Deserialize<QualityScenario>(text, Options)
           ?? throw new InvalidDataException($"empty or invalid quality scenario: {path}");
}
