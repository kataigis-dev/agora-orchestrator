namespace Agora.Orchestration;

/// <summary>The shared blackboard threaded through a graph run.</summary>
public sealed class State
{
    public State(string userInput) => UserInput = userInput;

    public string UserInput { get; }
    public List<Message> Messages { get; } = new();
    public Dictionary<string, string> Outputs { get; } = new();
    public Dictionary<string, object> Signals { get; set; } = new();
    public Dictionary<string, int> LoopCounters { get; } = new();
    public Dictionary<string, object> Artifacts { get; } = new();
    public string? LastAgent { get; set; }

    /// <summary>All message contents addressed to agentId, joined by blank lines.</summary>
    public string Inbox(string agentId) =>
        string.Join("\n\n", Messages.Where(m => m.Recipient == agentId).Select(m => m.Content));
}
