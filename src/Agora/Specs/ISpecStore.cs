namespace Agora.Specs;

/// <summary>
/// Persistence for the structured <see cref="SpecDocument"/> — the deterministic source of truth,
/// kept separate from fuzzy RAG retrieval. Built-in implementations: <see cref="FileSpecStore"/>
/// (JSON on disk, in the core) and a RAG-over-MCP store (in <c>Agora.AgentFramework</c>). Loads
/// return <see cref="SpecDocument.Empty"/> when nothing has been stored yet.
/// </summary>
public interface ISpecStore
{
    /// <summary>Loads the current specification (or <see cref="SpecDocument.Empty"/> if none).</summary>
    Task<SpecDocument> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists the specification, replacing any previously stored one.</summary>
    Task SaveAsync(SpecDocument document, CancellationToken cancellationToken = default);
}
