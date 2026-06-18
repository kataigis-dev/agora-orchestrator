using Agora.Agents;
using Agora.Observability;

namespace Agora.Orchestration;

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

        var original = Console.ForegroundColor;

        void Render(string msg) { Console.Out.WriteLine(msg); Console.Out.Flush(); }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Render("━━━ Agent Graph Execution ━━━");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Out.Write("  Graph: "); Console.Out.Flush();
        Console.ForegroundColor = ConsoleColor.White;
        var lines = new List<string>();
        foreach (var edge in _graph.Edges)
            lines.Add($"{edge.Source} ──{edge.Type}→ {edge.Target}");
        Render(string.Join("\n" + new string(' ', 9), lines));
        Render("");
        Console.ForegroundColor = original;

        var current = _graph.Entry;
        var steps = 0;
        while (current != Graph.End)
        {
            steps++;
            if (steps > _maxSteps)
                throw new ExecutionError($"exceeded max_steps={_maxSteps}");

            var node = _graph.Nodes[current];

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Out.Write($"  ▶ [{steps}] Agent: "); Console.Out.Flush();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Out.WriteLine(node.Id); Console.Out.Flush();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Render($"  └─ input: {Truncate(userInput, 50)}");
            Render("");
            Console.ForegroundColor = original;

            AgentResult result;
            using (Tracing.BeginSpan("node.run", new() { ["node"] = node.Id }))
            {
                var agent = _agentFactory(node.Id);
                var artifactsSummary = state.ArtifactSummary();
                var inbox = state.Inbox(node.Id);
                var context = string.IsNullOrEmpty(artifactsSummary) ? inbox
                    : string.IsNullOrEmpty(inbox) ? artifactsSummary
                    : $"{artifactsSummary}\n\n{inbox}";
                result = await agent.RunAsync(state.UserInput, context);
                state.Outputs[node.Id] = result.Output;
                state.Signals = new Dictionary<string, object>(result.Signals);
                foreach (var (key, value) in result.Artifacts)
                    state.Artifacts[key] = value;
                state.LastAgent = node.Id;
            }

            var next = NextNode(current, state);

            var signals = state.Signals.Count > 0
                ? string.Join(", ", state.Signals.Keys) : "none";
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Render($"  └─ signals: {signals}");
            Console.ForegroundColor = original;

            if (result.Artifacts.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                foreach (var (key, value) in result.Artifacts)
                    Render($"  └─ artifact {key}: {Truncate(value, 60)}");
                Console.ForegroundColor = original;
            }

            var edgeLabel = EdgeLabel(current, next, state);
            if (next != Graph.End)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Out.Write($"  ──▶ {edgeLabel} → "); Console.Out.Flush();
                Console.ForegroundColor = ConsoleColor.White;
                Render(next);
                Render("");
                Console.ForegroundColor = original;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Out.Write($"  ──▶ {edgeLabel} → "); Console.Out.Flush();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Render("END");
                Render("");
                Console.ForegroundColor = original;
            }

            if (next != Graph.End)
                state.Messages.Add(new Message(current, next, state.Outputs[current]));
            current = next;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Render("━━━ Execution Complete ━━━");
        Render("");
        Console.ForegroundColor = original;

        return state;
    }

    private string EdgeLabel(string source, string target, State state)
    {
        foreach (var edge in _graph.Edges)
        {
            if (edge.Source != source || edge.Target != target) continue;
            if (edge.Type == "conditional")
            {
                var loop = "";
                if (edge.MaxLoops is int max)
                {
                    var key = $"{edge.Source}->{edge.Target}";
                    var count = state.LoopCounters.GetValueOrDefault(key, 0);
                    loop = $" ({count}/{max})";
                }
                return $"condition:{edge.When}{loop}";
            }
            return edge.Type;
        }
        return "?";
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
            return edge.Target;
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

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}
