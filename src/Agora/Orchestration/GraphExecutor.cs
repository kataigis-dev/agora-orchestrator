using Agora.Agents;
using Agora.Observability;

namespace Agora.Orchestration;

public sealed class GraphExecutor
{
    private const string HandoffKey = "handoff";

    private readonly Graph _graph;
    private readonly Func<string, IAgent> _agentFactory;
    private readonly int _maxSteps;
    private readonly bool _handoff;
    private readonly Agora.Rag.ContextMemory? _memory;
    private readonly int _memoryTopK;

    public GraphExecutor(
        Graph graph, Func<string, IAgent> agentFactory, int maxSteps = 100, bool handoff = false,
        Agora.Rag.ContextMemory? memory = null, int memoryTopK = 5)
    {
        _graph = graph;
        _agentFactory = agentFactory;
        _maxSteps = maxSteps;
        _handoff = handoff;
        _memory = memory;
        _memoryTopK = memoryTopK;
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
                var inbox = state.Inbox(node.Id);
                // Memory mode compresses the shared channel: recall only the top-K relevant entries
                // instead of dumping every accumulated artifact.
                var shared = _memory is not null
                    ? await _memory.RecallAsync($"{state.UserInput}\n{inbox}", _memoryTopK)
                    : state.ArtifactSummary();
                var context = string.IsNullOrEmpty(shared) ? inbox
                    : string.IsNullOrEmpty(inbox) ? shared
                    : $"{shared}\n\n{inbox}";
                result = await agent.RunAsync(state.UserInput, context);
                state.Outputs[node.Id] = result.Output;
                state.Signals = new Dictionary<string, object>(result.Signals);
                foreach (var (key, value) in result.Artifacts)
                    // In handoff mode the 'handoff' artifact is a targeted channel to the next
                    // agent, not part of the globally-summarized shared artifacts.
                    if (!_handoff || key != HandoffKey)
                        state.Artifacts[key] = value;
                // Save the agent's declared artifacts (incl. handoff) as recallable memory.
                if (_memory is not null)
                    foreach (var (key, value) in result.Artifacts)
                        await _memory.RememberAsync($"{key}: {value}", node.Id);
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
            {
                if (_handoff)
                {
                    // Pass only the explicit handoff payload; if the agent emitted none, the next
                    // agent gets no inbox context (it must rely on RAG / shared artifacts instead).
                    if (result.Artifacts.TryGetValue(HandoffKey, out var payload) && !string.IsNullOrEmpty(payload))
                        state.Messages.Add(new Message(current, next, payload));
                }
                else
                {
                    state.Messages.Add(new Message(current, next, state.Outputs[current]));
                }
            }
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
