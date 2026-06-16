using System.Text;

namespace Agora.Rag;

public sealed record EnrichedInput
{
    public required string Original { get; init; }
    public required string RefinedQuery { get; init; }
    public IReadOnlyList<string> SubQueries { get; init; } = Array.Empty<string>();
    public IReadOnlyList<Chunk> Retrieved { get; init; } = Array.Empty<Chunk>();

    public string AsContext()
    {
        if (Retrieved.Count == 0)
            return "";
        var sb = new StringBuilder("Relevant context:");
        for (var i = 0; i < Retrieved.Count; i++)
            sb.Append('\n').Append($"[{i + 1}] ({Retrieved[i].Source}) {Retrieved[i].Text}");
        return sb.ToString();
    }
}
