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
    private readonly SkillRegistry _skillRegistry;
    private readonly IOutputInterpreter _interpreter;
    private readonly bool _h2c;

    public Runtime(
        AgoraConfig config,
        IChatProvider provider,
        string? configDir = null,
        RagPipeline? rag = null,
        IToolAgentFactory? toolAgentFactory = null,
        IApprovalHandler? approvalHandler = null)
    {
        _config = config;
        _provider = provider is ResilientChatProvider ? provider : new ResilientChatProvider(provider);
        _specs = ModelResolver.Resolve(config);
        _configDir = configDir;
        _toolAgentFactory = toolAgentFactory;
        _approvalHandler = approvalHandler;
        _skillRegistry = SkillLoader.Load(config.Skills?.Directories ?? new List<string>());
        _h2c = string.Equals(config.Communication, "h2c", StringComparison.OrdinalIgnoreCase);
        _interpreter = _h2c ? new H2cInterpreter() : new SignalInterpreter();
        Rag = rag ?? BuildRag();
    }

    public AgoraConfig Config => _config;
    public RagPipeline? Rag { get; }

    public static Runtime FromConfig(
        string path, IChatProvider provider, RagPipeline? rag = null,
        IToolAgentFactory? toolAgentFactory = null, IApprovalHandler? approvalHandler = null)
    {
        var full = Path.GetFullPath(path);
        return new Runtime(
            ConfigLoader.Load(full), provider, Path.GetDirectoryName(full), rag, toolAgentFactory, approvalHandler);
    }

    private RagPipeline? BuildRag()
    {
        var rag = _config.Rag;
        if (rag is null)
            return null;
        var refineModel = rag.Refine?.Model;
        ModelSpec? refineSpec = refineModel is not null && _specs.TryGetValue(refineModel, out var spec) ? spec : null;
        return RagFactory.Build(_config, _provider, refineSpec);
    }

    public async Task<AgentResult> RunAgentAsync(string agentId, string userInput)
    {
        using var _ = Tracing.BeginSpan("agent.run", new() { ["agent"] = agentId });
        return await BuildAgent(agentId).RunAsync(userInput);
    }

    public async Task<RunResult> RunAsync(string userInput)
    {
        var graph = GraphBuilder.Build(_config);
        GraphBuilder.Validate(graph, _config.Agents.Keys.ToHashSet());
        var executor = new GraphExecutor(graph, BuildAgent);

        EnrichedInput? enriched = null;
        var seedContext = "";
        if (Rag is not null)
        {
            enriched = await Rag.RunAsync(userInput);
            seedContext = enriched.AsContext();
        }

        using var _ = Tracing.BeginSpan("graph.run");
        var state = await executor.RunAsync(userInput, seedContext);
        var output = state.LastAgent is not null ? state.Outputs.GetValueOrDefault(state.LastAgent, "") : "";
        return new RunResult { Output = output, State = state, Enriched = enriched };
    }

    public IAgent BuildAgent(string agentId)
    {
        if (!_config.Agents.TryGetValue(agentId, out var agentConfig))
            throw new KeyNotFoundException($"unknown agent '{agentId}'");
        var alias = agentConfig.Model ?? _config.Defaults.Model
            ?? throw new KeyNotFoundException($"agent '{agentId}' has no model and no defaults.model");
        var spec = _specs[alias];
        if (agentConfig.Timeout is double timeout)
            spec = spec with { Timeout = timeout };
        var systemPrompt = ResolvePrompt(agentConfig);
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
