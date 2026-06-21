using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using System.Text;
using System.Text.RegularExpressions;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;

namespace Agora.Rag.Concretes;

/// <summary>
/// Uses an LLM to decide whether a new knowledge entry contradicts existing ones.
/// The model is asked to reply with a small line-based protocol that this class parses.
/// </summary>
public sealed partial class LlmConflictJudge : IConflictJudge
{
    private const string System = """
        You verify whether a NEW knowledge-base entry conflicts with EXISTING entries.
        A conflict means the new entry contradicts or is mutually exclusive with an existing one
        (not merely adds detail). Reply ONLY in this format:

        VERDICT: <NO_CONFLICT|RESOLVED|UNRESOLVED>
        CONFLICTS_WITH: <comma-separated numbers of the existing entries that conflict, or NONE>
        RESOLUTION: <only if RESOLVED: a single statement true to both old and new>
        EXPLANATION: <one short sentence>

        Use RESOLVED only when you can confidently merge both into one correct statement.
        Use UNRESOLVED when the contradiction needs a human to decide.
        """;

    private readonly IChatProvider _provider;
    private readonly ModelSpec _spec;

    /// <summary>Creates the judge backed by the given chat provider and model.</summary>
    public LlmConflictJudge(IChatProvider provider, ModelSpec spec)
    {
        _provider = provider;
        _spec = spec;
    }

    /// <inheritdoc />
    public async Task<ConflictAssessment> AssessAsync(
        string newEntry, IReadOnlyList<Chunk> existing, CancellationToken cancellationToken = default)
    {
        var prompt = new StringBuilder();
        prompt.Append("NEW ENTRY:\n").Append(newEntry).Append("\n\nEXISTING ENTRIES:\n");
        for (var i = 0; i < existing.Count; i++)
            prompt.Append(i + 1).Append(". ").Append(existing[i].Text).Append('\n');

        var messages = new[]
        {
            new ChatMessage("system", System),
            new ChatMessage("user", prompt.ToString()),
        };
        var result = await _provider.CompleteAsync(messages, _spec, cancellationToken);
        return Parse(result.Text, existing);
    }

    /// <summary>Parses the model's marker-protocol reply into a <see cref="ConflictAssessment"/>;
    /// anything unclassifiable is treated as no conflict.</summary>
    private static ConflictAssessment Parse(string text, IReadOnlyList<Chunk> existing)
    {
        var verdict = (Section(text, "VERDICT") ?? "").ToUpperInvariant();
        var explanation = Section(text, "EXPLANATION") ?? "";
        var conflicting = ConflictingEntries(Section(text, "CONFLICTS_WITH"), existing);

        if (verdict.Contains("UNRESOLVED"))
            return new ConflictAssessment(ConflictVerdict.Unresolved,
                Explanation: explanation.Length > 0 ? explanation : verdict, Conflicting: conflicting);

        if (verdict.Contains("RESOLVED"))
            return new ConflictAssessment(ConflictVerdict.Resolved,
                ResolvedText: Section(text, "RESOLUTION") ?? "",
                Explanation: explanation, Conflicting: conflicting);

        // NO_CONFLICT, or anything we cannot classify → treat as no conflict.
        return new ConflictAssessment(ConflictVerdict.NoConflict, Explanation: explanation);
    }

    /// <summary>Maps the "CONFLICTS_WITH" 1-based indices to existing chunks; falls back
    /// to all existing entries when the model gave no usable list.</summary>
    private static IReadOnlyList<Chunk> ConflictingEntries(string? raw, IReadOnlyList<Chunk> existing)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return existing;
        var picked = new List<Chunk>();
        foreach (var token in raw.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            if (int.TryParse(token, out var n) && n >= 1 && n <= existing.Count)
                picked.Add(existing[n - 1]);
        return picked.Count > 0 ? picked : existing;
    }

    /// <summary>Extracts the value after "MARKER:" — the rest of that line plus any following
    /// lines, until the next ALL-CAPS marker line.</summary>
    private static string? Section(string text, string marker)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();
        var capturing = false;
        foreach (var line in lines)
        {
            if (!capturing)
            {
                var idx = line.IndexOf(marker + ":", StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;
                capturing = true;
                var rest = line[(idx + marker.Length + 1)..].Trim();
                if (rest.Length > 0) sb.AppendLine(rest);
            }
            else
            {
                if (MarkerLine().IsMatch(line)) break;
                sb.AppendLine(line);
            }
        }
        var value = sb.ToString().Trim();
        return value.Length == 0 ? null : value;
    }

    /// <summary>Matches a line that begins a new ALL-CAPS marker (e.g. <c>EXPLANATION:</c>).</summary>
    [GeneratedRegex(@"^\s*[A-Z_]{3,}:")]
    private static partial Regex MarkerLine();
}
