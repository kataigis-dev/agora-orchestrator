using Agora.Agents;
using Agora.Observability;

namespace Agora.Orchestration;

/// <summary>
/// Drives a graph run: executes each node's agent, threads shared <see cref="State"/>, resolves the
/// next node (sequential/handoff/conditional/route edges), fans out parallel branches, and
/// checkpoints after every step. Supports handoff mode and RAG-backed context memory.
/// </summary>
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
    private readonly IExecutionObserver _observer;
    private const int OutputMemoryCap = 2000;

    /// <summary>Creates an executor for a graph, with optional handoff mode, context memory, LLM
    /// router, checkpoint store, run id, a streaming token sink, and an execution observer
    /// (defaults to <see cref="ConsoleExecutionObserver"/>).</summary>
    public GraphExecutor(
        Graph graph, Func<string, IAgent> agentFactory, int maxSteps = 100, bool handoff = false,
        Agora.Rag.ContextMemory? memory = null, Agora.Rag.MemoryOptions? memoryOptions = null,
        IRouter? router = null, ICheckpointStore? checkpoints = null, string? runId = null,
        Action<string>? onChunk = null, IExecutionObserver? observer = null)
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
        _observer = observer ?? new ConsoleExecutionObserver();
    }

    /// <summary>Runs the graph from the entry node (or resumes from <paramref name="resumeFrom"/>) until
    /// it reaches END or exceeds the step limit, returning the final shared state.</summary>
    /// <param name="userInput">The task/input for the run.</param>
    /// <param name="seedContext">Optional context (e.g. RAG results) delivered to the entry node's inbox.</param>
    /// <param name="resumeFrom">Optional snapshot to resume a previously checkpointed run.</param>
    public async Task<State> RunAsync(string userInput, string seedContext = "", StateSnapshot? resumeFrom = null)
    {
        var state = resumeFrom?.ToState() ?? new State(userInput);
        if (resumeFrom is null && !string.IsNullOrEmpty(seedContext))
            state.Messages.Add(new Message("rag", _graph.Entry, seedContext));

        _observer.OnGraphStart(_graph);

        var current = resumeFrom?.Current ?? _graph.Entry;
        var steps = resumeFrom?.Steps ?? 0;
        while (current != Graph.End)
        {
            steps++;
            if (steps > _maxSteps)
                throw new ExecutionError($"exceeded max_steps={_maxSteps}");

            var node = _graph.Nodes[current];
            _observer.OnNodeStart(steps, node.Id, userInput);

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
                current = await FanOutAsync(current, parallelTargets, state);
                Checkpoint(state, current, steps);
                continue;
            }

            var next = await NextNodeAsync(current, state);

            _observer.OnSignals(state.Signals.Keys.ToList());
            foreach (var (key, value) in result.Artifacts)
                _observer.OnArtifact(key, value);
            _observer.OnEdge(EdgeLabel(current, next, state), next, next == Graph.End);

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

        _observer.OnGraphComplete();
        return state;
    }

    /// <summary>Saves a snapshot of the run after a step, if a checkpoint store is configured.</summary>
    private void Checkpoint(State state, string current, int steps)
        => _checkpoints?.Save(_runId, StateSnapshot.From(state, current, steps));

    /// <summary>Builds a node's context from its inbox plus shared background — recalled memory in
    /// memory mode, otherwise the full artifact summary.</summary>
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

    /// <summary>Builds and runs a single node's agent with its assembled context.</summary>
    private async Task<AgentResult> RunOnceAsync(string nodeId, State state, Action<string>? onChunk = null)
    {
        var agent = _agentFactory(nodeId);
        var context = await BuildContextAsync(nodeId, state);
        return await agent.RunAsync(state.UserInput, context, onChunk);
    }

    /// <summary>Writes a node's artifacts (and, when enabled, its truncated output) to context memory.</summary>
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
        string fork, IReadOnlyList<string> branches, State state)
    {
        // Deliver the fork's output to each branch's inbox before running them.
        foreach (var branch in branches)
            state.Messages.Add(new Message(fork, branch, state.Outputs[fork]));
        _observer.OnParallel(branches);

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

    /// <summary>Builds a human-readable label for the edge taken (including loop counts for
    /// conditional edges), used for console rendering.</summary>
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

    /// <summary>Picks the next node by signals: takes the first matching conditional edge (honoring
    /// <c>max_loops</c>) or the first unconditional edge; returns END if none apply.</summary>
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

    /// <summary>True when the named signal is present and truthy (non-false bool, non-empty string).</summary>
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

    /// <summary>Shortens a string to at most <paramref name="max"/> characters, appending an ellipsis.</summary>
    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}
