namespace Agora.Configuration;

/// <summary>Opt-in RAG-backed context memory: instead of injecting every shared artifact
/// into each agent, save declared artifacts and recall only the top-K relevant ones.</summary>
public sealed class MemoryConfig
{
    public bool Enabled { get; set; } = true;

    /// <summary>How many relevant memory entries to recall into an agent's context.</summary>
    public int TopK { get; set; } = 5;
}
