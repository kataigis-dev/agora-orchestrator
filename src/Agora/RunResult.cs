using Agora.Orchestration;
using Agora.Rag;

namespace Agora;

/// <summary>The result of a runtime run: final output plus the full end state.</summary>
public sealed record RunResult
{
    /// <summary>The final output text.</summary>
    public required string Output { get; init; }

    /// <summary>The shared state at the end of the run.</summary>
    public required State State { get; init; }

    /// <summary>The RAG-enriched input, when retrieval ran.</summary>
    public EnrichedInput? Enriched { get; init; }

    /// <summary>The run id checkpoints are stored under (when checkpointing is enabled).</summary>
    public string? RunId { get; init; }

    /// <summary>Aggregate run metrics (steps, rework, token/cache usage); null for non-graph runs.</summary>
    public RunMetrics? Metrics { get; init; }
}
