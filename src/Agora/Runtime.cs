using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using Agora.Communication;
using Agora.Configuration;
using Agora.HumanInTheLoop;
using Agora.Observability;
using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using Agora.Skills;
using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Specs.Concretes;
using Agora.Verification.Contracts;
using Agora.Verification.Models;
using Agora.Verification.Concretes;

namespace Agora;

/// <summary>
/// The composition root: from a config it resolves models, builds the RAG pipeline, shared
/// knowledge base, context memory, and graph, and exposes entry points to run a single agent, run
/// or resume a graph. Wraps the provider with resilience and injects the communication/handoff/
/// language preambles into each agent.
/// </summary>
public sealed class Runtime
{
    private readonly AgoraConfig _config;
    private readonly IChatProvider _provider;
    private readonly Dictionary<string, ModelSpec> _specs;
    private readonly string? _configDir;
    private readonly IAgentBackend? _backend;
    private readonly IApprovalHandler? _approvalHandler;
    private readonly IConflictResolver? _conflictResolver;
    private readonly SkillRegistry _skillRegistry;
    private readonly AgentInstructions _instructions;
    private readonly bool _handoff;
    private readonly Retrieval? _retrieval;
    private readonly IRouter? _router;
    private readonly ICheckpointStore? _checkpoints;
    private readonly IExecutionObserver? _observer;

    /// <summary>Builds the runtime from a config and provider, wiring optional edge dependencies
    /// (the agent backend, HITL handlers, checkpoint store).</summary>
    public Runtime(
        AgoraConfig config,
        IChatProvider provider,
        string? configDir = null,
        Retrieval? retrieval = null,
        IAgentBackend? backend = null,
        IApprovalHandler? approvalHandler = null,
        IConflictResolver? conflictResolver = null,
        ICheckpointStore? checkpointStore = null,
        IExecutionObserver? observer = null)
    {
        _config = config;
        // Fail loud at build: a writable KB (rag_write) with a fake/missing embedder or unresolvable
        // judge model would silently no-op the conflict check. Enforced here too so a directly-built
        // Runtime (not via the `validate` verb) can't bypass it.
        RagWriteValidator.Validate(config);
        _checkpoints = checkpointStore;
        _observer = observer;
        _provider = provider is ResilientChatProvider ? provider : new ResilientChatProvider(provider);
        _specs = ModelResolver.Resolve(config);
        _configDir = configDir;
        _backend = backend;
        _approvalHandler = approvalHandler;
        _conflictResolver = conflictResolver;
        _skillRegistry = SkillLoader.Load(config.Skills?.Directories ?? new List<string>());
        _handoff = config.Handoff == true;
        _instructions = AgentInstructions.For(config);
        // One module owns the embedder/store and the pipeline → knowledge base → memory build order.
        // Every KB mutation is recorded to an append-only audit log next to the config (purgeable).
        _retrieval = retrieval ?? global::Agora.Rag.Concretes.Retrieval.Build(
            _config, _provider, _backend, _conflictResolver, BuildMutationLog());
        SpecStore = SpecStoreFactory.Build(_config, _configDir, _backend);
        CheckRunner = BuildCheckRunner();
        _router = _config.Defaults.Model is string routerAlias && _specs.TryGetValue(routerAlias, out var routerSpec)
            ? new LlmRouter(_provider, routerSpec)
            : null;
    }

    /// <summary>The loaded configuration.</summary>
    public AgoraConfig Config => _config;

    /// <summary>The retrieval subsystem (read pipeline + knowledge base + context memory), or null when
    /// both RAG and memory are disabled.</summary>
    public Retrieval? Retrieval => _retrieval;

    /// <summary>The RAG read pipeline, or null when RAG is disabled.</summary>
    public RagPipeline? Rag => _retrieval?.Pipeline;

    /// <summary>Write path into the shared knowledge base (same store as <see cref="Rag"/>), or null when RAG is off.</summary>
    public KnowledgeBase? KnowledgeBase => _retrieval?.KnowledgeBase;

    /// <summary>Structured spec store (source of truth for the <c>spec_*</c> tools), or null when SDD is off.</summary>
    public ISpecStore? SpecStore { get; }

    /// <summary>Runs allow-listed build/test checks, or null when no <c>checks</c> are configured.</summary>
    public ICheckRunner? CheckRunner { get; }

    /// <summary>Loads the config at <paramref name="path"/> and builds a runtime from it.</summary>
    public static Runtime FromConfig(
        string path, IChatProvider provider, Retrieval? retrieval = null,
        IAgentBackend? backend = null, IApprovalHandler? approvalHandler = null,
        IConflictResolver? conflictResolver = null,
        ICheckpointStore? checkpointStore = null,
        IExecutionObserver? observer = null)
    {
        var full = Path.GetFullPath(path);
        return new Runtime(
            ConfigLoader.Load(full), provider, Path.GetDirectoryName(full), retrieval, backend,
            approvalHandler, conflictResolver, checkpointStore, observer);
    }

    /// <summary>Builds the append-only KB mutation log (next to the config) when RAG is enabled, so every
    /// knowledge-base add/replace is auditable; null when RAG is off. The file is created lazily on the
    /// first mutation, so a read-only KB leaves no log behind.</summary>
    private IKbMutationLog? BuildMutationLog()
        => _config.Rag is { Enabled: true }
            ? new FileKbMutationLog(FileKbMutationLog.DefaultPath(_configDir ?? Directory.GetCurrentDirectory()))
            : null;

    /// <summary>Builds the check runner from config (real process execution of allow-listed commands);
    /// null when no <c>checks</c> are configured.</summary>
    private ICheckRunner? BuildCheckRunner()
        => _config.Checks is { } checks
            ? new ProcessCheckRunner(checks, _configDir ?? Directory.GetCurrentDirectory())
            : null;

    /// <summary>Runs a single agent (no graph) on the input, optionally streaming its output. Returns the
    /// same <see cref="RunResult"/> shape as <see cref="RunAsync"/> — the run's output, a minimal end
    /// state, and one-step <see cref="RunMetrics"/> projected from the agent's own token counts — so every
    /// run is measured the same way (no graph machinery on this lean path).</summary>
    public async Task<RunResult> RunAgentAsync(string agentId, string userInput, Action<string>? onChunk = null)
    {
        using var _ = Tracing.BeginSpan("agent.run", new() { ["agent"] = agentId });
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await BuildAgent(agentId).RunAsync(userInput, "", onChunk);
        stopwatch.Stop();

        var state = new State(userInput) { LastAgent = agentId, Signals = result.Signals };
        state.Outputs[agentId] = result.Output;
        foreach (var (key, value) in result.Artifacts)
            state.Artifacts[key] = value;

        var metrics = await WithTraceability(RunMetrics.ForAgent(result, stopwatch.Elapsed, agentId));
        return new RunResult { Output = result.Output, State = state, Metrics = metrics };
    }

    /// <summary>Runs the configured graph: seeds RAG context if enabled, executes to completion, and
    /// returns the result (with a run id when checkpointing).</summary>
    public async Task<RunResult> RunAsync(string userInput, string? runId = null, Action<string>? onChunk = null)
    {
        var id = runId ?? (_checkpoints is not null ? Guid.NewGuid().ToString("N") : "run");
        var metrics = new MetricsExecutionObserver();
        var executor = BuildExecutor(id, onChunk, Observe(metrics));

        EnrichedInput? enriched = null;
        var seedContext = "";
        if (Rag is not null)
        {
            enriched = await Rag.RunAsync(userInput);
            seedContext = enriched.AsContext();
        }

        using var _ = Tracing.BeginSpan("graph.run");
        var state = await executor.RunAsync(userInput, seedContext);
        return BuildResult(state, id, enriched, await WithTraceability(metrics.Metrics));
    }

    /// <summary>Resumes a previously checkpointed run from where it left off.</summary>
    public async Task<RunResult> ResumeAsync(string runId)
    {
        if (_checkpoints is null)
            throw new InvalidOperationException("resume requires a checkpoint store");
        var snapshot = _checkpoints.Load(runId)
            ?? throw new KeyNotFoundException($"no checkpoint found for run '{runId}'");

        using var _ = Tracing.BeginSpan("graph.resume");
        var metrics = new MetricsExecutionObserver();
        var state = await BuildExecutor(runId, null, Observe(metrics))
            .RunAsync(snapshot.State.UserInput, resumeFrom: snapshot);
        return BuildResult(state, runId, enriched: null, await WithTraceability(metrics.Metrics));
    }

    /// <summary>Enriches run metrics with the spec completion verdict when a spec store is configured.
    /// Best-effort: a store read failure (e.g. an unreachable MCP server) leaves metrics untouched
    /// rather than failing the run.</summary>
    private async Task<RunMetrics> WithTraceability(RunMetrics metrics)
    {
        if (SpecStore is null)
            return metrics;
        try
        {
            var report = TraceabilityValidator.Analyze(await SpecStore.LoadAsync());
            return metrics with
            {
                Traceability = new TraceabilitySummary
                {
                    Requirements = report.InScopeCount,
                    Covered = report.CoveredCount,
                    Verified = report.VerifiedCount,
                    Tasks = report.TaskCount,
                    Complete = report.IsComplete,
                },
            };
        }
        catch
        {
            return metrics;
        }
    }

    /// <summary>Composes the configured presentation observer with the run's metrics collector, so every
    /// graph run is measured while library callers stay silent unless they opt into rendering.</summary>
    private IExecutionObserver Observe(MetricsExecutionObserver metrics)
        => new CompositeExecutionObserver(_observer ?? NullExecutionObserver.Instance, metrics);

    /// <summary>Builds and validates the graph and wraps it in an executor wired with handoff/memory/
    /// router/checkpoint settings.</summary>
    private GraphExecutor BuildExecutor(string runId, Action<string>? onChunk, IExecutionObserver observer)
    {
        var graph = GraphBuilder.Build(_config);
        GraphBuilder.Validate(graph, _config.Agents.Keys.ToHashSet());
        var memoryOptions = _config.Memory is { } m
            ? new MemoryOptions(m.TopK, m.MaxChars, m.RememberOutputs)
            : new MemoryOptions();
        // Mint a fresh, empty context memory per run: run-time context is per-run and discarded at run
        // end (on resume the executor re-seeds it from the checkpoint's artifacts).
        return new GraphExecutor(graph, BuildAgent, handoff: _handoff,
            memory: _retrieval?.NewMemory(), memoryOptions: memoryOptions, router: _router,
            checkpoints: _checkpoints, runId: runId, onChunk: onChunk, observer: observer);
    }

    /// <summary>Assembles a <see cref="RunResult"/> from the end state, taking the last agent's output.</summary>
    private static RunResult BuildResult(State state, string runId, EnrichedInput? enriched, RunMetrics? metrics)
    {
        var output = state.LastAgent is not null ? state.Outputs.GetValueOrDefault(state.LastAgent, "") : "";
        return new RunResult { Output = output, State = state, Enriched = enriched, RunId = runId, Metrics = metrics };
    }

    /// <summary>Builds the runnable agent for an id (used as the executor's agent factory).</summary>
    public IAgent BuildAgent(string agentId) => BuildAgent(agentId, answerMode: false);

    /// <summary>Re-runs a target agent to answer another agent's <c>ask_agent</c> tool call.
    /// The target is built in answer-mode so it cannot ask back (prevents A↔B recursion).</summary>
    private async Task<string> AskAgentAsync(string target, string question)
    {
        if (!_config.Agents.ContainsKey(target))
            return $"error: unknown agent '{target}'";
        var result = await BuildAgent(target, answerMode: true).RunAsync(question);
        return result.Output;
    }

    /// <summary>Builds an agent: resolves its model/prompt, prepends the handoff/H2C/language
    /// preambles, and returns either a tool-capable agent (if it declares skills/tools) or a plain
    /// one. In answer-mode the handoff preamble and <c>ask_agent</c> tool are omitted.</summary>
    private IAgent BuildAgent(string agentId, bool answerMode)
    {
        if (!_config.Agents.TryGetValue(agentId, out var agentConfig))
            throw new KeyNotFoundException($"unknown agent '{agentId}'");
        var alias = agentConfig.Model ?? _config.Defaults.Model
            ?? throw new KeyNotFoundException($"agent '{agentId}' has no model and no defaults.model");
        var spec = _specs[alias];
        if (agentConfig.Timeout is double timeout)
            spec = spec with { Timeout = timeout };
        var systemPrompt = ResolvePrompt(agentConfig);
        // One module assembles the language/protocol/handoff prefix (handoff is skipped in answer-mode,
        // where the agent answers an ask_agent question rather than producing a handoff).
        var prefix = _instructions.Prefix(answerMode);
        if (!string.IsNullOrEmpty(prefix))
            systemPrompt = string.IsNullOrEmpty(systemPrompt) ? prefix : prefix + "\n\n" + systemPrompt;
        var card = new AgentCard
        {
            Id = agentId,
            Model = alias,
            Role = agentConfig.Role,
            SystemPrompt = systemPrompt,
            Skills = agentConfig.Skills,
            Tools = agentConfig.Tools,
            Approvals = agentConfig.Approvals,
        };

        if (agentConfig.Skills.Count > 0 || agentConfig.Tools.Count > 0)
        {
            if (_backend is null)
                throw new InvalidOperationException(
                    $"agent '{agentId}' declares skills/tools but no IAgentBackend was provided");
            if (agentConfig.Approvals.Count > 0 && _approvalHandler is null)
                throw new InvalidOperationException(
                    $"agent '{agentId}' declares approvals but no IApprovalHandler was provided");
            var skills = _skillRegistry.ForAgent(agentConfig.Skills);
            return _backend.CreateToolAgent(new AgentBuildContext
            {
                Card = card,
                Spec = spec,
                Skills = skills,
                Approvals = agentConfig.Approvals,
                ApprovalHandler = _approvalHandler,
                Interpreter = _instructions.Interpreter,
                Mcp = _config.Mcp,
                FilesystemRoot = _configDir ?? Directory.GetCurrentDirectory(),
                Rag = Rag,
                KnowledgeBase = KnowledgeBase,
                AskAgent = answerMode ? null : AskAgentAsync,
                SpecStore = SpecStore,
                SpecRequireCriteria = _config.Spec?.RequireCriteria ?? true,
                CheckRunner = CheckRunner,
            });
        }

        return new Agent(card, _provider, spec, _instructions.Interpreter);
    }

    /// <summary>Resolves an agent's system prompt from its inline value or prompt file (relative to
    /// the config directory), or empty when neither is set.</summary>
    private string ResolvePrompt(AgentConfig agentConfig)
    {
        if (!string.IsNullOrEmpty(agentConfig.SystemPrompt))
            return agentConfig.SystemPrompt;
        if (!string.IsNullOrEmpty(agentConfig.SystemPromptFile))
        {
            var baseDir = _configDir ?? Directory.GetCurrentDirectory();
            return File.ReadAllText(Path.Combine(baseDir, agentConfig.SystemPromptFile));
        }
        return "";
    }
}
