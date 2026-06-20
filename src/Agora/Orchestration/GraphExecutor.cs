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
    private readonly Agora.Rag.MemoryOptions _memoryOptions;
    private readonly IRouter? _router;
    private readonly ICheckpointStore? _checkpoints;
    private readonly string _runId;
    private readonly Action<string>? _onChunk;
    private const int OutputMemoryCap = 2000;

    public GraphExecutor(
        Graph graph, Func<string, IAgent> agentFactory, int maxSteps = 100, bool handoff = false,
        Agora.Rag.ContextMemory? memory = null, Agora.Rag.MemoryOptions? memoryOptions = null,
        IRouter? router = null, ICheckpointStore? checkpoints = null, string? runId = null,
        Action<string>? onChunk = null)
    {
        _graph = graph;
        _agentFactory = agentFactory;
        _maxSteps = maxSteps;
        _handoff = handoff;
        _memory = memory;
        _memoryOptions = memoryOptions ?? new Agora.Rag.MemoryOptions();
        _router = router;
        _checkpoints = checkpoints;
        _runId = runId ?? "run";
        _onChunk = onChunk;
    }

    public async Task<State> RunAsync(string userInput, string seedContext = "", StateSnapshot? resumeFrom = null)
    {
        var state = resumeFrom?.ToState() ?? new State(userInput);
        if (resumeFrom is null && !string.IsNullOrEmpty(seedContext))
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

        var current = resumeFrom?.Current ?? _graph.Entry;
        var steps = resumeFrom?.Steps ?? 0;
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
                result = await RunOnceAsync(node.Id, state, _onChunk);
                state.Outputs[node.Id] = result.Output;
                state.Signals = new Dictionary<string, object>(result.Signals);
                foreach (var (key, value) in result.Artifacts)
                    // In handoff mode the 'handoff' artifact is a targeted channel to the next
                    // agent, not part of the globally-summarized shared artifacts.
                    if (!_handoff || key != HandoffKey)
                        state.Artifacts[key] = value;
                await RememberAsync(node.Id, result, state);
                state.LastAgent = node.Id;
            }

            // Fan-out: a node whose outgoing edges are 'parallel' runs all branches concurrently,
            // then continues from the single node they converge on.
            var parallelTargets = _graph.Edges
                .Where(e => e.Source == current && e.Type == "parallel")
                .Select(e => e.Target).Distinct().ToList();
            if (parallelTargets.Count > 0)
            {
                current = await FanOutAsync(current, parallelTargets, state, Render);
                Checkpoint(state, current, steps);
                continue;
            }

            var next = await NextNodeAsync(current, state);

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
            Checkpoint(state, current, steps);
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Render("━━━ Execution Complete ━━━");
        Render("");
        Console.ForegroundColor = original;

        return state;
    }

    private void Checkpoint(State state, string current, int steps)
        => _checkpoints?.Save(_runId, StateSnapshot.From(state, current, steps));

    private async Task<string> BuildContextAsync(string nodeId, State state)
    {
        var inbox = state.Inbox(nodeId);
        // Memory mode compresses the shared channel: recall only the top-K relevant entries
        // instead of dumping every accumulated artifact.
        var shared = _memory is not null
            ? await _memory.RecallAsync($"{state.UserInput}\n{inbox}", _memoryOptions.TopK, _memoryOptions.MaxChars)
            : state.ArtifactSummary();
        return string.IsNullOrEmpty(shared) ? inbox
            : string.IsNullOrEmpty(inbox) ? shared
            : $"{shared}\n\n{inbox}";
    }

    private async Task<AgentResult> RunOnceAsync(string nodeId, State state, Action<string>? onChunk = null)
    {
        var agent = _agentFactory(nodeId);
        var context = await BuildContextAsync(nodeId, state);
        return await agent.RunAsync(state.UserInput, context, onChunk);
    }

    private async Task RememberAsync(string nodeId, AgentResult result, State state)
    {
        if (_memory is null)
            return;
        foreach (var (key, value) in result.Artifacts)
            await _memory.RememberAsync($"{key}: {value}", nodeId);
        if (_memoryOptions.RememberOutputs && !string.IsNullOrWhiteSpace(result.Output))
            await _memory.RememberAsync(Truncate(result.Output, OutputMemoryCap), nodeId);
    }

    /// <summary>Runs the fork's parallel branches concurrently (agent calls run at the same time),
    /// records their results serially, and returns the single node they converge on.</summary>
    private async Task<string> FanOutAsync(
        string fork, IReadOnlyList<string> branches, State state, Action<string> render)
    {
        // Deliver the fork's output to each branch's inbox before running them.
        foreach (var branch in branches)
            state.Messages.Add(new Message(fork, branch, state.Outputs[fork]));
        render($"  ⇉ parallel: {string.Join(", ", branches)}");
        render("");

        // Concurrency is in the (expensive) agent calls; state is mutated only after they all finish.
        var results = await Task.WhenAll(branches.Select(async branch =>
        {
            using (Tracing.BeginSpan("branch.run", new() { ["node"] = branch }))
                return (Node: branch, Result: await RunOnceAsync(branch, state));
        }));

        var joins = branches
            .SelectMany(b => _graph.Edges.Where(e => e.Source == b).Select(e => e.Target))
            .Distinct().ToList();
        var join = joins.Count switch
        {
            0 => Graph.End,
            1 => joins[0],
            _ => throw new GraphError(
                $"parallel branches [{string.Join(", ", branches)}] must converge on one join node, "
                + $"found [{string.Join(", ", joins)}]"),
        };

        foreach (var (node, result) in results)
        {
            state.Outputs[node] = result.Output;
            foreach (var (key, value) in result.Artifacts)
                if (!_handoff || key != HandoffKey)
                    state.Artifacts[key] = value;
            await RememberAsync(node, result, state);
            state.LastAgent = node;
            if (join != Graph.End)
                state.Messages.Add(new Message(node, join, result.Output));
        }
        return join;
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

    /// <summary>Resolves the next node, using the LLM router for <c>route</c> edges and the
    /// signal-based <see cref="NextNode"/> otherwise.</summary>
    private async Task<string> NextNodeAsync(string source, State state)
    {
        var routeEdges = _graph.Edges.Where(e => e.Source == source && e.Type == "route").ToList();
        if (routeEdges.Count == 0 || _router is null)
            return NextNode(source, state);

        var options = routeEdges.Select(e => new RouteOption(e.Target, e.When ?? e.Target)).ToList();
        return await _router.ChooseAsync(state.Outputs.GetValueOrDefault(source, ""), options);
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
