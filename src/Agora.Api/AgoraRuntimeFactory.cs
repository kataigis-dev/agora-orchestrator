using Agora.Agents;
using Agora.Configuration;
using Agora.HumanInTheLoop;
using Agora.Providers;
using Agora.Rag;

namespace Agora.Api;

/// <summary>Holds the shared, startup-built dependencies and builds a fresh Runtime per run.</summary>
public sealed class AgoraRuntimeFactory
{
    private readonly string? _configDir;
    private readonly IChatProvider _provider;
    private readonly IToolAgentFactory _toolAgentFactory;

    public AgoraRuntimeFactory(
        AgoraConfig config, string? configDir, IChatProvider provider,
        IToolAgentFactory toolAgentFactory, RagPipeline? rag)
    {
        Config = config;
        _configDir = configDir;
        _provider = provider;
        _toolAgentFactory = toolAgentFactory;
        Rag = rag;
    }

    public AgoraConfig Config { get; }
    public RagPipeline? Rag { get; }

    public Runtime Build(IApprovalHandler approvalHandler)
        => new(Config, _provider, _configDir, Rag, _toolAgentFactory, approvalHandler);
}
