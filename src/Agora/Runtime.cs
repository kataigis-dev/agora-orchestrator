using Agora.Agents;
using Agora.Communication;
using Agora.Configuration;
using Agora.HumanInTheLoop;
using Agora.Observability;
using Agora.Orchestration;
using Agora.Providers;
using Agora.Rag;
using Agora.Skills;

namespace Agora;

public sealed class Runtime
{
    private readonly AgoraConfig _config;
    private readonly IChatProvider _provider;
    private readonly Dictionary<string, ModelSpec> _specs;
    private readonly string? _configDir;
    private readonly IToolAgentFactory? _toolAgentFactory;
    private readonly IApprovalHandler? _approvalHandler;
    private readonly IConflictResolver? _conflictResolver;
    private readonly Func<Configuration.VectorStoreConfig?, IVectorStore?>? _storeResolver;
    private readonly Func<EmbedderSpec, IEmbedder?>? _embedderResolver;
    private readonly SkillRegistry _skillRegistry;
    private readonly IOutputInterpreter _interpreter;
    private readonly bool _h2c;
    private readonly bool _handoff;
    private readonly ContextMemory? _memory;
    private readonly IRouter? _router;
    private readonly ICheckpointStore? _checkpoints;

    public Runtime(
        AgoraConfig config,
        IChatProvider provider,
        string? configDir = null,
        RagPipeline? rag = null,
        IToolAgentFactory? toolAgentFactory = null,
        IApprovalHandler? approvalHandler = null,
        IConflictResolver? conflictResolver = null,
        Func<Configuration.VectorStoreConfig?, IVectorStore?>? storeResolver = null,
        Func<EmbedderSpec, IEmbedder?>? embedderResolver = null,
        ICheckpointStore? checkpointStore = null)
    {
        _config = config;
        _checkpoints = checkpointStore;
        _provider = provider is ResilientChatProvider ? provider : new ResilientChatProvider(provider);
        _specs = ModelResolver.Resolve(config);
        _configDir = configDir;
        _toolAgentFactory = toolAgentFactory;
        _approvalHandler = approvalHandler;
        _conflictResolver = conflictResolver;
        _storeResolver = storeResolver;
        _embedderResolver = embedderResolver;
        _skillRegistry = SkillLoader.Load(config.Skills?.Directories ?? new List<string>());
        _h2c = string.Equals(config.Communication, "h2c", StringComparison.OrdinalIgnoreCase);
        _handoff = config.Handoff == true;
        _interpreter = _h2c ? new H2cInterpreter() : new SignalInterpreter();
        Rag = rag ?? BuildRag();
        KnowledgeBase = BuildKnowledgeBase();
        _memory = BuildMemory();
        _router = _config.Defaults.Model is string routerAlias && _specs.TryGetValue(routerAlias, out var routerSpec)
            ? new LlmRouter(_provider, routerSpec)
            : null;
    }

    public AgoraConfig Config => _config;
    public RagPipeline? Rag { get; }

    /// <summary>Write path into the shared knowledge base (same store as <see cref="Rag"/>), or null when RAG is off.</summary>
    public KnowledgeBase? KnowledgeBase { get; }

    public static Runtime FromConfig(
        string path, IChatProvider provider, RagPipeline? rag = null,
        IToolAgentFactory? toolAgentFactory = null, IApprovalHandler? approvalHandler = null,
        IConflictResolver? conflictResolver = null,
        Func<Configuration.VectorStoreConfig?, IVectorStore?>? storeResolver = null,
        Func<EmbedderSpec, IEmbedder?>? embedderResolver = null,
        ICheckpointStore? checkpointStore = null)
    {
        var full = Path.GetFullPath(path);
        return new Runtime(
            ConfigLoader.Load(full), provider, Path.GetDirectoryName(full), rag, toolAgentFactory,
            approvalHandler, conflictResolver, storeResolver, embedderResolver, checkpointStore);
    }

    private RagPipeline? BuildRag()
    {
        var rag = _config.Rag;
        if (rag is null)
            return null;
        var refineModel = rag.Refine?.Model;
        ModelSpec? refineSpec = refineModel is not null && _specs.TryGetValue(refineModel, out var spec) ? spec : null;
        return RagFactory.Build(_config, _provider, refineSpec, _storeResolver, _embedderResolver);
    }

    private KnowledgeBase? BuildKnowledgeBase()
    {
        if (Rag is null)
            return null;
        // Reuse the read pipeline's embedder + store so reads and writes share one knowledge base.
        IConflictJudge judge = _config.Defaults.Model is string alias && _specs.TryGetValue(alias, out var spec)
            ? new LlmConflictJudge(_provider, spec)
            : new NoOpConflictJudge();
        return new KnowledgeBase(Rag.Embedder, Rag.Store, judge, _conflictResolver);
    }

    private ContextMemory? BuildMemory()
    {
        if (_config.Memory?.Enabled != true)
            return null;
        // Reuse the RAG embedder/store when available (entries are source-tagged to stay
        // distinct from KB facts); otherwise fall back to an offline in-memory store.
        var embedder = Rag?.Embedder ?? new FakeEmbedder();
        var store = Rag?.Store ?? new InMemoryVectorStore();
        return new ContextMemory(embedder, store);
    }

    public async Task<AgentResult> RunAgentAsync(string agentId, string userInput, Action<string>? onChunk = null)
    {
        using var _ = Tracing.BeginSpan("agent.run", new() { ["agent"] = agentId });
        return await BuildAgent(agentId).RunAsync(userInput, "", onChunk);
    }

    public async Task<RunResult> RunAsync(string userInput, string? runId = null, Action<string>? onChunk = null)
    {
        var id = runId ?? (_checkpoints is not null ? Guid.NewGuid().ToString("N") : "run");
        var executor = BuildExecutor(id, onChunk);

        EnrichedInput? enriched = null;
        var seedContext = "";
        if (Rag is not null)
        {
            enriched = await Rag.RunAsync(userInput);
            seedContext = enriched.AsContext();
        }

        using var _ = Tracing.BeginSpan("graph.run");
        var state = await executor.RunAsync(userInput, seedContext);
        return BuildResult(state, id, enriched);
    }

    /// <summary>Resumes a previously checkpointed run from where it left off.</summary>
    public async Task<RunResult> ResumeAsync(string runId)
    {
        if (_checkpoints is null)
            throw new InvalidOperationException("resume requires a checkpoint store");
        var snapshot = _checkpoints.Load(runId)
            ?? throw new KeyNotFoundException($"no checkpoint found for run '{runId}'");

        using var _ = Tracing.BeginSpan("graph.resume");
        var state = await BuildExecutor(runId).RunAsync(snapshot.UserInput, resumeFrom: snapshot);
        return BuildResult(state, runId, enriched: null);
    }

    private GraphExecutor BuildExecutor(string runId, Action<string>? onChunk = null)
    {
        var graph = GraphBuilder.Build(_config);
        GraphBuilder.Validate(graph, _config.Agents.Keys.ToHashSet());
        var memoryOptions = _config.Memory is { } m
            ? new MemoryOptions(m.TopK, m.MaxChars, m.RememberOutputs)
            : new MemoryOptions();
        return new GraphExecutor(graph, BuildAgent, handoff: _handoff,
            memory: _memory, memoryOptions: memoryOptions, router: _router,
            checkpoints: _checkpoints, runId: runId, onChunk: onChunk);
    }

    private static RunResult BuildResult(State state, string runId, EnrichedInput? enriched)
    {
        var output = state.LastAgent is not null ? state.Outputs.GetValueOrDefault(state.LastAgent, "") : "";
        return new RunResult { Output = output, State = state, Enriched = enriched, RunId = runId };
    }

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
        // The handoff preamble is about producing a handoff; it is noise when an agent is merely
        // answering an ask_agent question, so skip it in answer-mode.
        if (_handoff && !answerMode)
            systemPrompt = HandoffPreamble.For(_h2c) + "\n\n" + systemPrompt;
        if (_h2c)
            systemPrompt = H2cPreamble.Text + "\n\n" + systemPrompt;
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
            if (_toolAgentFactory is null)
                throw new InvalidOperationException(
                    $"agent '{agentId}' declares skills/tools but no IToolAgentFactory was provided");
            if (agentConfig.Approvals.Count > 0 && _approvalHandler is null)
                throw new InvalidOperationException(
                    $"agent '{agentId}' declares approvals but no IApprovalHandler was provided");
            var skills = _skillRegistry.ForAgent(agentConfig.Skills);
            return _toolAgentFactory.Create(new AgentBuildContext
            {
                Card = card,
                Spec = spec,
                Skills = skills,
                Approvals = agentConfig.Approvals,
                ApprovalHandler = _approvalHandler,
                Interpreter = _interpreter,
                Mcp = _config.Mcp,
                Rag = Rag,
                KnowledgeBase = KnowledgeBase,
                AskAgent = answerMode ? null : AskAgentAsync,
            });
        }

        return new Agent(card, _provider, spec, _interpreter);
    }

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
