namespace Agora.Orchestration;

/// <summary>A graph node. <paramref name="Type"/> is one of <c>agent</c>, <c>human</c>, or <c>end</c>
/// (<c>human</c> is reserved for a later phase).</summary>
/// <param name="Id">Node id (matches an agent id for agent nodes).</param>
/// <param name="Type">Node kind.</param>
public sealed record Node(string Id, string Type = "agent");
