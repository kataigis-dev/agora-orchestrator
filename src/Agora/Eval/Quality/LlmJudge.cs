using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Agora.Providers.Contracts;
using Agora.Providers.Models;

namespace Agora.Eval.Quality;

/// <summary>
/// An LLM-as-judge: grades an output against a rubric on the anchored 0/0.5/1 scale, asking for a short
/// justification and cited evidence before each score. It requests a constrained JSON reply, parses it
/// tolerantly (with a marker-protocol fallback for weak models), and is fail-CLOSED — an unparseable or
/// incomplete verdict yields <see cref="JudgeVerdict.Invalid"/>, never a free pass (contrast
/// <see cref="Agora.Rag.Concretes.LlmConflictJudge"/>, which fails open).
/// </summary>
public sealed class LlmJudge : IJudge
{
    private const string System = """
        You are a strict evaluation judge. Grade the ASSISTANT OUTPUT against the listed dimensions.
        For EACH dimension, choose a score from EXACTLY this set: 0, 0.5, or 1.
          1   = fully meets the criterion
          0.5 = partially meets it
          0   = fails it
        Before scoring, write a one-sentence justification and cite concrete evidence from the output
        (or, for groundedness, from the provided context). Be conservative: if the evidence is missing
        or you are unsure, score lower.

        Reply with ONLY a JSON object — no prose, no markdown fences — in exactly this shape:
        { "<dimension>": { "justification": "...", "evidence": "...", "score": 0 } }
        Include every listed dimension and no others.
        """;

    private readonly IChatProvider _provider;
    private readonly ModelSpec _spec;

    /// <summary>Creates the judge over a provider and model spec. Temperature is forced to 0 for
    /// reproducibility regardless of the spec passed in.</summary>
    public LlmJudge(IChatProvider provider, ModelSpec spec)
    {
        _provider = provider;
        _spec = spec with { Temperature = 0 };
    }

    /// <inheritdoc />
    public async Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default)
    {
        var messages = new[]
        {
            new ChatMessage("system", System),
            new ChatMessage("user", BuildUserPrompt(request)),
        };
        var result = await _provider.CompleteAsync(messages, _spec, cancellationToken);
        return Parse(result.Text, request.Rubric.Keys);
    }

    /// <summary>Builds the grading prompt from the request: dimensions+criteria, the input, the output, and
    /// the optional reference / grounding context.</summary>
    private static string BuildUserPrompt(JudgeRequest request)
    {
        var sb = new StringBuilder();
        sb.Append("TASK INPUT:\n").Append(request.Input).Append("\n\n");
        sb.Append("ASSISTANT OUTPUT:\n").Append(request.Output).Append("\n\n");
        if (!string.IsNullOrWhiteSpace(request.Reference))
            sb.Append("REFERENCE ANSWER (for comparison):\n").Append(request.Reference).Append("\n\n");
        if (!string.IsNullOrWhiteSpace(request.GroundingContext))
            sb.Append("RETRIEVED CONTEXT (judge groundedness ONLY against this):\n")
              .Append(request.GroundingContext).Append("\n\n");
        sb.Append("GRADE THESE DIMENSIONS (score each 0, 0.5, or 1):\n");
        foreach (var (dim, criteria) in request.Rubric)
            sb.Append("- ").Append(dim).Append(": ").Append(criteria).Append('\n');
        sb.Append("\nReply with ONLY the JSON object.");
        return sb.ToString();
    }

    /// <summary>Parses the judge reply fail-closed: tries JSON first, then a marker fallback; returns an
    /// invalid verdict if any requested dimension lacks a well-formed score in {0, 0.5, 1}.</summary>
    internal static JudgeVerdict Parse(string text, IEnumerable<string> dimensions)
    {
        var dims = dimensions.ToList();
        var json = ExtractJsonObject(text);
        if (json is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var parsed = new Dictionary<string, DimensionVerdict>();
                foreach (var dim in dims)
                {
                    if (!TryGetProperty(root, dim, out var el) || el.ValueKind != JsonValueKind.Object)
                        return JudgeVerdict.Invalid($"missing dimension '{dim}'");
                    if (!TryGetProperty(el, "score", out var scoreEl) || !TryScore(scoreEl, out var score))
                        return JudgeVerdict.Invalid($"dimension '{dim}' has no valid 0/0.5/1 score");
                    parsed[dim] = new DimensionVerdict(score, GetString(el, "justification"), GetString(el, "evidence"));
                }
                return new JudgeVerdict { Dimensions = parsed, Valid = true };
            }
            catch (JsonException)
            {
                // Fall through to the marker fallback.
            }
        }
        return MarkerFallback(text, dims);
    }

    /// <summary>Last-resort parse for models that won't emit clean JSON: looks for "dimension: score" lines.</summary>
    private static JudgeVerdict MarkerFallback(string text, List<string> dims)
    {
        var parsed = new Dictionary<string, DimensionVerdict>();
        foreach (var dim in dims)
        {
            var m = Regex.Match(text, Regex.Escape(dim) + @"\s*[:=]\s*(1|0\.5|0)", RegexOptions.IgnoreCase);
            if (!m.Success)
                return JudgeVerdict.Invalid($"could not parse a score for '{dim}'");
            parsed[dim] = new DimensionVerdict(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), "", "");
        }
        return new JudgeVerdict { Dimensions = parsed, Valid = true };
    }

    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }

    private static bool TryGetProperty(JsonElement obj, string name, out JsonElement value)
    {
        value = default;
        if (obj.ValueKind != JsonValueKind.Object)
            return false;
        foreach (var p in obj.EnumerateObject())
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        return false;
    }

    private static string GetString(JsonElement obj, string name)
        => TryGetProperty(obj, name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    /// <summary>Accepts only the anchored values 0, 0.5, 1 (number or numeric string); anything else fails closed.</summary>
    private static bool TryScore(JsonElement el, out double score)
    {
        score = 0;
        double v;
        if (el.ValueKind == JsonValueKind.Number)
            v = el.GetDouble();
        else if (el.ValueKind == JsonValueKind.String
                 && double.TryParse(el.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            v = parsed;
        else
            return false;
        foreach (var allowed in new[] { 0.0, 0.5, 1.0 })
            if (Math.Abs(v - allowed) < 0.001)
            {
                score = allowed;
                return true;
            }
        return false;
    }
}
