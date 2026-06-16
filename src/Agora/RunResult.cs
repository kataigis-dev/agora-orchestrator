using Agora.Orchestration;
using Agora.Rag;

namespace Agora;

public sealed record RunResult
{
    public required string Output { get; init; }
    public required State State { get; init; }
    public EnrichedInput? Enriched { get; init; }
}
