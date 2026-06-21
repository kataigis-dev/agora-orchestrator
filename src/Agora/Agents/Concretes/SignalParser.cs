using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using System.Text.RegularExpressions;

namespace Agora.Agents.Concretes;

/// <summary>
/// Extracts &lt;&lt;signal name&gt;&gt; / &lt;&lt;signal name=value&gt;&gt; control tokens and
/// &lt;&lt;artifact key=value&gt;&gt; data tokens from agent output. Both are stripped from the
/// returned text. Shared by the core Agent and MAF-backed tool agents.
/// </summary>
public static class SignalParser
{
    private static readonly Regex SignalRegex =
        new(@"<<signal\s+([a-zA-Z_]\w*)(?:=([^>]*))?>>", RegexOptions.Compiled);

    private static readonly Regex ArtifactRegex =
        new(@"<<artifact\s+([a-zA-Z_]\w*)=([^>]+)>>", RegexOptions.Compiled);

    /// <summary>
    /// Extracts signals, artifacts, and cleaned text from raw agent output.
    /// Signals with no value default to <c>true</c>. Artifacts always require a value.
    /// </summary>
    public static (string Output, Dictionary<string, object> Signals, Dictionary<string, string> Artifacts) Extract(string text)
    {
        var signals = new Dictionary<string, object>();
        foreach (Match match in SignalRegex.Matches(text))
        {
            var name = match.Groups[1].Value;
            signals[name] = match.Groups[2].Success ? match.Groups[2].Value.Trim() : (object)true;
        }

        var artifacts = new Dictionary<string, string>();
        foreach (Match match in ArtifactRegex.Matches(text))
            artifacts[match.Groups[1].Value] = match.Groups[2].Value.Trim();

        var cleaned = SignalRegex.Replace(text, "");
        cleaned = ArtifactRegex.Replace(cleaned, "").Trim();
        return (cleaned, signals, artifacts);
    }
}
