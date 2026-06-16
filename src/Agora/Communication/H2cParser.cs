using System.Text;
using System.Text.RegularExpressions;

namespace Agora.Communication;

/// <summary>
/// Lenient parser/serializer for the H2C block grammar (https://github.com/LuPaLa-Coder/H2C):
/// a <c>[TIPO:SOTTOTIPO]</c> header optionally followed by a <c>key:value|key:value</c> line.
/// Surrounding prose is ignored; malformed blocks are skipped.
/// </summary>
public sealed class H2cParser
{
    private static readonly Regex Header = new(@"^\[(?<t>[A-Za-z]+):(?<s>[A-Za-z0-9_]+)\]$", RegexOptions.Compiled);

    private static readonly HashSet<string> Types = new(StringComparer.Ordinal)
        { "ARCH", "BUILD", "TEST", "CTX", "STATE", "ORCH", "SKILL" };

    private static readonly HashSet<string> Subtypes = new(StringComparer.Ordinal)
        { "PLAN", "EXEC", "DONE", "FIX", "REVERT", "NACK", "RUN", "PASS", "FAIL", "PRIMITIVES",
          "UPDATE", "PRUNE", "COMPACT", "FREEZE", "NEGOTIATE", "FINDINGS", "ACK", "END", "PROMPT" };

    public IReadOnlyList<H2cBlock> Parse(string text)
    {
        var blocks = new List<H2cBlock>();
        if (string.IsNullOrEmpty(text))
            return blocks;
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var header = Header.Match(lines[i].Trim());
            if (!header.Success)
                continue;

            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            var j = i + 1;
            while (j < lines.Length && lines[j].Trim().Length == 0)
                j++;
            if (j < lines.Length)
            {
                var candidate = lines[j].Trim();
                if (!Header.IsMatch(candidate) && candidate.Contains(':'))
                {
                    foreach (var pair in candidate.Split('|'))
                    {
                        var idx = pair.IndexOf(':');
                        if (idx <= 0)
                            continue;
                        var key = pair[..idx].Trim();
                        if (key.Length > 0)
                            fields[key] = pair[(idx + 1)..].Trim();
                    }
                    i = j; // consume the fields line
                }
            }

            blocks.Add(new H2cBlock
            {
                Type = header.Groups["t"].Value,
                Subtype = header.Groups["s"].Value,
                Fields = fields,
            });
        }
        return blocks;
    }

    public string Serialize(IEnumerable<H2cBlock> blocks)
    {
        var sb = new StringBuilder();
        foreach (var b in blocks)
        {
            sb.Append('[').Append(b.Type).Append(':').Append(b.Subtype).Append("]\n");
            if (b.Fields.Count > 0)
                sb.AppendJoin('|', b.Fields.Select(kv => $"{kv.Key}:{kv.Value}")).Append('\n');
        }
        return sb.ToString().TrimEnd('\n');
    }

    public bool IsValid(H2cBlock block) => Types.Contains(block.Type) && Subtypes.Contains(block.Subtype);
}
