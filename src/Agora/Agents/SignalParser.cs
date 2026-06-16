using System.Text.RegularExpressions;

namespace Agora.Agents;

/// <summary>
/// Extracts &lt;&lt;signal name&gt;&gt; / &lt;&lt;signal name=value&gt;&gt; control tokens from agent
/// output (bare token -> true, token with value -> the value string) and returns the cleaned,
/// stripped text alongside the signals. Shared by the core Agent and MAF-backed tool agents.
/// </summary>
public static class SignalParser
{
    private static readonly Regex SignalRegex =
        new(@"<<signal\s+([a-zA-Z_]\w*)(?:=([^>]*))?>>", RegexOptions.Compiled);

    public static (string Output, Dictionary<string, object> Signals) Extract(string text)
    {
        var signals = new Dictionary<string, object>();
        foreach (Match match in SignalRegex.Matches(text))
        {
            var name = match.Groups[1].Value;
            signals[name] = match.Groups[2].Success ? match.Groups[2].Value.Trim() : (object)true;
        }
        var cleaned = SignalRegex.Replace(text, "").Trim();
        return (cleaned, signals);
    }
}
