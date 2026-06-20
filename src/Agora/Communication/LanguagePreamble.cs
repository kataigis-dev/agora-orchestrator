namespace Agora.Communication;

/// <summary>System-prompt preamble fixing the language agents must use when generating
/// documents and responses (config: <c>language</c>).</summary>
public static class LanguagePreamble
{
    /// <summary>Returns the output-language directive for the given language name.</summary>
    public static string For(string language)
        => $"OUTPUT LANGUAGE: write all generated documents, artifacts, and responses in {language}.";
}
