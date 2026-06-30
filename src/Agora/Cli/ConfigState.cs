using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using Agora.Configuration;
using Agora.HumanInTheLoop;
using Agora.Orchestration.Contracts;
using Agora.Orchestration.Concretes;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Specs.Concretes;

namespace Agora.Cli;

/// <summary>Parsed CLI options bundled with the injected I/O streams and edge dependencies
/// (provider, the agent backend, and HITL handlers) that each command needs.</summary>
internal record ConfigState(Dictionary<string, string> Options, IChatProvider? Provider, IAgentBackend? Backend, IApprovalHandler? ApprovalHandler,
    IConflictResolver? ConflictResolver,
    TextReader In, TextWriter Out, TextWriter Error, bool Interactive = true,
    IRagMcpServer? McpServer = null)
{
    /// <summary>Returns a required option's value, or throws if it's missing.</summary>
    public string Require(string name)
        => Options.TryGetValue(name, out var value)
            ? value
            : throw new ConfigException($"missing required option --{name}");

    /// <summary>The read-only RAG MCP server, or throws if none was supplied.</summary>
    public IRagMcpServer RequireMcpServer()
        => McpServer ?? throw new InvalidOperationException("no MCP server supplied");

    /// <summary>Refuses to proceed when a run could reach <c>rag_write</c> — whose conflict gate blocks on
    /// a human — in a non-interactive process (stdin redirected / no TTY). Agora is CLI-only with a human
    /// always present, so this is refused rather than silently degraded to a dropped write. Best-effort:
    /// it cannot be made cryptographically impossible.</summary>
    public void RequireInteractiveForRagWrite(AgoraConfig config)
    {
        if (Interactive)
            return;
        var writer = config.Agents.FirstOrDefault(a => a.Value.Tools.Contains("rag_write"));
        if (writer.Key is not null)
            throw new ConfigException(
                $"agent '{writer.Key}' can use rag_write, whose conflict resolution needs a human, but "
                + "this process is not interactive (stdin is redirected / no TTY). Agora is CLI-only and a "
                + "human must be present — run it in an interactive terminal.");
    }

    /// <summary>The chat provider, or throws if no provider was supplied.</summary>
    public IChatProvider RequireProvider()
        => Provider ?? throw new InvalidOperationException("no chat provider supplied");

    /// <summary>Builds a runtime from the <c>--config</c> option and the supplied provider, wired with the
    /// edge dependencies (agent backend, HITL handlers, and an optional checkpoint store). The single place
    /// CLI verbs get a ready-to-run runtime.</summary>
    public Runtime BuildRuntime(ICheckpointStore? checkpoints = null)
        => Runtime.FromConfig(Require("config"), RequireProvider(),
            backend: Backend, approvalHandler: ApprovalHandler,
            conflictResolver: ConflictResolver, checkpointStore: checkpoints,
            observer: new ConsoleExecutionObserver());
}