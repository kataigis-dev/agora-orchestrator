using System.Text;

namespace Agora.Rag;

/// <summary>
/// RAG-backed working memory for context compression. Agents' declared artifacts are
/// <see cref="RememberAsync"/>-ed (append-only, no conflict check), and only the top-K most
/// relevant entries are <see cref="RecallAsync"/>-ed into the next agent's context — capping
/// token usage on long graphs while keeping what matters. Entries are tagged with a
/// <see cref="SourcePrefix"/> so they stay distinct from knowledge-base facts in a shared store.
/// </summary>
public sealed class ContextMemory
{
    public const string SourcePrefix = "memory:";

    private readonly IEmbedder _embedder;
    private readonly IVectorStore _store;

    public ContextMemory(IEmbedder embedder, IVectorStore store)
    {
        _embedder = embedder;
        _store = store;
    }

    public async Task RememberAsync(string text, string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        var vector = (await _embedder.EmbedAsync(new[] { text }, cancellationToken))[0];
        _store.Upsert(new[] { new Chunk(text, SourcePrefix + agentId) }, new[] { vector });
    }

    /// <summary>Returns a formatted block of the top-K memory entries most relevant to the query,
    /// or empty when there is nothing relevant.</summary>
    public async Task<string> RecallAsync(string query, int topK, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || topK <= 0)
            return "";
        var vector = (await _embedder.EmbedAsync(new[] { query }, cancellationToken))[0];
        // Over-fetch then keep only memory entries, so a shared store's KB facts don't crowd them out.
        var hits = _store.Query(vector, Math.Max(topK * 4, 20), 0.0)
            .Where(c => c.Source.StartsWith(SourcePrefix, StringComparison.Ordinal))
            .Take(topK)
            .ToList();
        if (hits.Count == 0)
            return "";

        var sb = new StringBuilder("Relevant context:");
        foreach (var hit in hits)
            sb.Append("\n- ").Append(hit.Text);
        return sb.ToString();
    }
}
