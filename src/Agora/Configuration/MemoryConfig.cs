namespace Agora.Configuration;

/// <summary>Opt-in RAG-backed context memory: instead of injecting every shared artifact
/// into each agent, save declared artifacts and recall only the top-K relevant ones.</summary>
public sealed class MemoryConfig
{
    /// <summary>Whether context memory is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How many relevant memory entries to recall into an agent's context.</summary>
    public int TopK { get; set; } = 5;

    /// <summary>Cap on the recalled context size in characters (0 = unlimited).</summary>
    public int MaxChars { get; set; }

    /// <summary>Also remember each agent's (truncated) output, not only its declared artifacts —
    /// so memory does not depend on the agent emitting artifacts.</summary>
    public bool RememberOutputs { get; set; }
}
