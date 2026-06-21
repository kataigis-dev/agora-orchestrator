using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using System.Text;

namespace Agora.Rag.Concretes;

/// <summary>Tuning for RAG-backed context memory.</summary>
/// <param name="TopK">How many relevant entries to recall.</param>
/// <param name="MaxChars">Cap on the recalled context size in characters (0 = unlimited).</param>
/// <param name="RememberOutputs">Also remember each agent's (truncated) output, not only declared artifacts.</param>
public sealed record MemoryOptions(int TopK = 5, int MaxChars = 0, bool RememberOutputs = false);

/// <summary>
/// RAG-backed working memory for context compression. Agents' declared artifacts are
/// <see cref="RememberAsync"/>-ed (append-only, no conflict check), and only the top-K most
/// relevant entries are <see cref="RecallAsync"/>-ed into the next agent's context — capping
/// token usage on long graphs while keeping what matters. Entries are tagged with a
/// <see cref="SourcePrefix"/> so they stay distinct from knowledge-base facts in a shared store.
/// </summary>
public sealed class ContextMemory
{
    /// <summary>Source prefix tagging memory entries so they stay distinct from knowledge-base facts.</summary>
    public const string SourcePrefix = "memory:";

    private readonly IEmbedder _embedder;
    private readonly IVectorStore _store;

    /// <summary>Creates context memory over a shared embedder and vector store.</summary>
    public ContextMemory(IEmbedder embedder, IVectorStore store)
    {
        _embedder = embedder;
        _store = store;
    }

    /// <summary>Stores a memory entry for an agent (append-only, no conflict check); no-ops on blank text.</summary>
    public async Task RememberAsync(string text, string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        var vector = (await _embedder.EmbedAsync(new[] { text }, cancellationToken))[0];
        await _store.UpsertAsync(new[] { new Chunk(text, SourcePrefix + agentId) }, new[] { vector }, cancellationToken);
    }

    /// <summary>Returns a formatted block of the top-K memory entries most relevant to the query,
    /// or empty when there is nothing relevant.</summary>
    public async Task<string> RecallAsync(
        string query, int topK, int maxChars = 0, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || topK <= 0)
            return "";
        var vector = (await _embedder.EmbedAsync(new[] { query }, cancellationToken))[0];
        // Over-fetch then keep only memory entries, so a shared store's KB facts don't crowd them out.
        var hits = (await _store.QueryAsync(vector, Math.Max(topK * 4, 20), 0.0, cancellationToken))
            .Where(c => c.Source.StartsWith(SourcePrefix, StringComparison.Ordinal))
            .Take(topK)
            .ToList();
        if (hits.Count == 0)
            return "";

        const string header = "Relevant context:";
        var sb = new StringBuilder(header);
        for (var i = 0; i < hits.Count; i++)
        {
            var line = "\n- " + hits[i].Text;
            // Keep at least the most relevant entry; otherwise stop at the char budget.
            if (maxChars > 0 && i > 0 && sb.Length + line.Length > maxChars)
                break;
            sb.Append(line);
        }
        return sb.ToString();
    }
}
