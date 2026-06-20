using System.Text.Json;

namespace Agora.Orchestration;

/// <summary>
/// A JSON-serializable snapshot of a graph run: enough to resume from the node about to execute.
/// Signals (a <c>string|bool</c> map) round-trip through JSON and are normalized back on load.
/// </summary>
public sealed class StateSnapshot
{
    public string Current { get; set; } = Graph.End;
    public int Steps { get; set; }
    public string UserInput { get; set; } = "";
    public List<Message> Messages { get; set; } = new();
    public Dictionary<string, string> Outputs { get; set; } = new();
    public Dictionary<string, int> LoopCounters { get; set; } = new();
    public Dictionary<string, string> Artifacts { get; set; } = new();
    public Dictionary<string, object> Signals { get; set; } = new();
    public string? LastAgent { get; set; }

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

    // After JSON round-trip, object values arrive as JsonElement; restore the bool/string/number type.
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
