using Agora.Agents;
using Agora.Observability;

namespace Agora.Orchestration;

/// <summary>Walks a Graph, running one agent per node and routing along edges.</summary>
public sealed class GraphExecutor
{
    private readonly Graph _graph;
    private readonly Func<string, IAgent> _agentFactory;
    private readonly int _maxSteps;

    public GraphExecutor(Graph graph, Func<string, IAgent> agentFactory, int maxSteps = 100)
    {
        _graph = graph;
        _agentFactory = agentFactory;
        _maxSteps = maxSteps;
    }

    public async Task<State> RunAsync(string userInput, string seedContext = "")
    {
        var state = new State(userInput);
        if (!string.IsNullOrEmpty(seedContext))
            state.Messages.Add(new Message("rag", _graph.Entry, seedContext));
        var current = _graph.Entry;
        var steps = 0;
        while (current != Graph.End)
        {
            steps++;
            if (steps > _maxSteps)
                throw new ExecutionError($"exceeded max_steps={_maxSteps}");

            var node = _graph.Nodes[current];
            using (Tracing.BeginSpan("node.run", new() { ["node"] = node.Id }))
            {
                var agent = _agentFactory(node.Id);
                var context = state.Inbox(node.Id);
                var result = await agent.RunAsync(state.UserInput, context);
                state.Outputs[node.Id] = result.Output;
                state.Signals = new Dictionary<string, object>(result.Signals);
                state.LastAgent = node.Id;
            }

            var next = NextNode(current, state);
            if (next != Graph.End)
                state.Messages.Add(new Message(current, next, state.Outputs[current]));
            current = next;
        }
        return state;
    }

    private string NextNode(string source, State state)
    {
        foreach (var edge in _graph.Edges)
        {
            if (edge.Source != source) continue;
            if (edge.Type == "conditional")
            {
                if (!IsTruthy(state.Signals, edge.When)) continue;
                if (edge.MaxLoops is int max)
                {
                    var key = $"{edge.Source}->{edge.Target}";
                    var count = state.LoopCounters.GetValueOrDefault(key, 0);
                    if (count >= max) continue;
                    state.LoopCounters[key] = count + 1;
                }
                return edge.Target;
            }
            return edge.Target; // sequential/handoff: default, always matches
        }
        return Graph.End;
    }

    private static bool IsTruthy(IReadOnlyDictionary<string, object> signals, string? key)
    {
        if (key is null || !signals.TryGetValue(key, out var value))
            return false;
        return value switch
        {
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            _ => true,
        };
    }
}
