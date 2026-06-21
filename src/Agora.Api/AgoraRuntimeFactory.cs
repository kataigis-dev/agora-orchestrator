using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using Agora.Configuration;
using Agora.HumanInTheLoop;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;

namespace Agora.Api;

/// <summary>Holds the shared, startup-built dependencies and builds a fresh Runtime per run.</summary>
public sealed class AgoraRuntimeFactory
{
    private readonly string? _configDir;
    private readonly IChatProvider _provider;
    private readonly IToolAgentFactory _toolAgentFactory;

    /// <summary>Captures the startup-built dependencies shared across all runs.</summary>
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

    /// <summary>The loaded configuration.</summary>
    public AgoraConfig Config { get; }

    /// <summary>The shared RAG pipeline, or null when RAG is disabled.</summary>
    public RagPipeline? Rag { get; }

    /// <summary>Builds a fresh runtime for one run, wired with the given per-run approval handler.</summary>
    public Runtime Build(IApprovalHandler approvalHandler)
        => new(Config, _provider, _configDir, Rag, _toolAgentFactory, approvalHandler);
}
