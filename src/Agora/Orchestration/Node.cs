namespace Agora.Orchestration;

// type: "agent" | "human" | "end" (human handled in a later phase)
public sealed record Node(string Id, string Type = "agent");
