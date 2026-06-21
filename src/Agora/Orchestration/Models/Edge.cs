using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
namespace Agora.Orchestration.Models;

/// <summary>A directed edge in the execution graph.</summary>
/// <param name="Source">Source node id.</param>
/// <param name="Target">Target node id, or <see cref="Graph.End"/>.</param>
/// <param name="Type">Edge kind: <c>sequential</c>, <c>handoff</c>, <c>conditional</c>, <c>route</c>, or <c>parallel</c>.</param>
/// <param name="When">Activating signal/condition for <c>conditional</c> and <c>route</c> edges.</param>
/// <param name="MaxLoops">Cap on how many times a <c>conditional</c> loop edge may fire.</param>
public sealed record Edge(
    string Source,
    string Target,
    string Type = "sequential",
    string? When = null,
    int? MaxLoops = null);
