using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using System.Text;

namespace Agora.Rag.Models;

/// <summary>The original input augmented with the refined query and retrieved context chunks.</summary>
public sealed record EnrichedInput
{
    /// <summary>The raw user input.</summary>
    public required string Original { get; init; }

    /// <summary>The query actually used for retrieval after refinement.</summary>
    public required string RefinedQuery { get; init; }

    /// <summary>Optional decomposed sub-queries.</summary>
    public IReadOnlyList<string> SubQueries { get; init; } = Array.Empty<string>();

    /// <summary>Chunks retrieved from the vector store.</summary>
    public IReadOnlyList<Chunk> Retrieved { get; init; } = Array.Empty<Chunk>();

    /// <summary>Formats the retrieved chunks as a numbered context block, or empty if none.</summary>
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
