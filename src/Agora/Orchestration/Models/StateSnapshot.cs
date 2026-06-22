using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
using System.Text.Json;

namespace Agora.Orchestration.Models;

/// <summary>
/// A JSON-serializable checkpoint: the run cursor (<see cref="Current"/> / <see cref="Steps"/>) plus
/// the <see cref="State"/> it was captured at. The state is the single source of truth — the snapshot
/// adds only the cursor, so a new state field flows through here automatically with no mirror to keep
/// in sync. Object-valued signals/artifacts come back as <see cref="JsonElement"/> after a round-trip
/// and are restored to their primitive types on <see cref="ToState"/>.
/// </summary>
public sealed class StateSnapshot
{
    /// <summary>Id of the node about to execute when the snapshot was taken.</summary>
    public string Current { get; set; } = Graph.End;

    /// <summary>Number of steps already executed.</summary>
    public int Steps { get; set; }

    /// <summary>The run state captured at the snapshot.</summary>
    public State State { get; set; } = new("");

    /// <summary>Captures a snapshot from live state, the next node, and the step count.</summary>
    public static StateSnapshot From(State state, string current, int steps)
        => new() { Current = current, Steps = steps, State = state };

    /// <summary>Returns the captured state, restoring signal/artifact value types after a JSON round-trip.</summary>
    public State ToState()
    {
        NormalizeMap(State.Signals);
        NormalizeMap(State.Artifacts);
        return State;
    }

    /// <summary>Restores each value's primitive type when it arrived as a <see cref="JsonElement"/>.</summary>
    private static void NormalizeMap(IDictionary<string, object> map)
    {
        foreach (var key in map.Keys.ToList())
            map[key] = NormalizeValue(map[key]);
    }

    /// <summary>After a JSON round-trip an <c>object</c> value arrives as <see cref="JsonElement"/>;
    /// this restores the original bool/string/number type.</summary>
    private static object NormalizeValue(object value) => value switch
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
