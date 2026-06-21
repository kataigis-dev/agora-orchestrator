using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
namespace Agora.Orchestration.Models;

/// <summary>The shared blackboard threaded through a graph run.</summary>
public sealed class State
{
    /// <summary>Creates a state seeded with the run's user input.</summary>
    public State(string userInput) => UserInput = userInput;

    /// <summary>The original user input / task for the run.</summary>
    public string UserInput { get; }

    /// <summary>Inter-agent messages exchanged during the run.</summary>
    public List<Message> Messages { get; } = new();

    /// <summary>Each agent's latest output, keyed by agent id.</summary>
    public Dictionary<string, string> Outputs { get; } = new();

    /// <summary>Signals from the most recent agent step, used for routing.</summary>
    public Dictionary<string, object> Signals { get; set; } = new();

    /// <summary>Per-edge loop counters enforcing <c>max_loops</c>.</summary>
    public Dictionary<string, int> LoopCounters { get; } = new();

    /// <summary>Shared artifacts accumulated across the run.</summary>
    public Dictionary<string, object> Artifacts { get; } = new();

    /// <summary>Id of the agent that ran most recently.</summary>
    public string? LastAgent { get; set; }

    /// <summary>All message contents addressed to agentId, joined by blank lines.</summary>
    public string Inbox(string agentId) =>
        string.Join("\n\n", Messages.Where(m => m.Recipient == agentId).Select(m => m.Content));

    /// <summary>A formatted summary of shared artifacts, or empty if none exist.</summary>
    public string ArtifactSummary()
    {
        if (Artifacts.Count == 0) return "";
        var lines = Artifacts.Select(kv => $"  {kv.Key}: {kv.Value}");
        return $"━━━ Shared Artifacts ━━━\n{string.Join("\n", lines)}";
    }
}
