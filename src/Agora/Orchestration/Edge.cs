namespace Agora.Orchestration;

// type: "sequential" | "handoff" | "conditional"
public sealed record Edge(
    string Source,
    string Target,
    string Type = "sequential",
    string? When = null,
    int? MaxLoops = null);
