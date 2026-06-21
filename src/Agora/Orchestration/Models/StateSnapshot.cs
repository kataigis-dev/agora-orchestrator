using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
using System.Text.Json;

namespace Agora.Orchestration.Models;

/// <summary>
/// A JSON-serializable snapshot of a graph run: enough to resume from the node about to execute.
/// Signals (a <c>string|bool</c> map) round-trip through JSON and are normalized back on load.
/// </summary>
public sealed class StateSnapshot
{
    /// <summary>Id of the node about to execute when the snapshot was taken.</summary>
    public string Current { get; set; } = Graph.End;

    /// <summary>Number of steps already executed.</summary>
    public int Steps { get; set; }
    public string UserInput { get; set; } = "";
    public List<Message> Messages { get; set; } = new();
    public Dictionary<string, string> Outputs { get; set; } = new();
    public Dictionary<string, int> LoopCounters { get; set; } = new();
    public Dictionary<string, string> Artifacts { get; set; } = new();
    public Dictionary<string, object> Signals { get; set; } = new();
    public string? LastAgent { get; set; }

    /// <summary>Captures a snapshot from live state, the next node, and the step count.</summary>
    public static StateSnapshot From(State state, string current, int steps) => new()
    {
        Current = current,
        Steps = steps,
        UserInput = state.UserInput,
        Messages = state.Messages.ToList(),
        Outputs = new Dictionary<string, string>(state.Outputs),
        LoopCounters = new Dictionary<string, int>(state.LoopCounters),
        Artifacts = state.Artifacts.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? ""),
        Signals = new Dictionary<string, object>(state.Signals),
        LastAgent = state.LastAgent,
    };

    /// <summary>Rebuilds live <see cref="State"/> from the snapshot, restoring signal value types.</summary>
    public State ToState()
    {
        var state = new State(UserInput) { LastAgent = LastAgent };
        state.Messages.AddRange(Messages);
        foreach (var (key, value) in Outputs) state.Outputs[key] = value;
        foreach (var (key, value) in LoopCounters) state.LoopCounters[key] = value;
        foreach (var (key, value) in Artifacts) state.Artifacts[key] = value;
        foreach (var (key, value) in Signals) state.Signals[key] = Normalize(value);
        return state;
    }

    /// <summary>After a JSON round-trip signal values arrive as <see cref="JsonElement"/>; this restores
    /// the original bool/string/number type.</summary>
    private static object Normalize(object value) => value switch
    {
        JsonElement je => je.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => je.GetString()!,
            JsonValueKind.Number => je.GetDouble(),
            _ => je.ToString(),
        },
        _ => value,
    };
}
