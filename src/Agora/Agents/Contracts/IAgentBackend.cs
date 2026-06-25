using Agora.Agents.Models;
using Agora.Configuration;
using Agora.HumanInTheLoop;
using Agora.Providers.Models;
using Agora.Rag.Concretes;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Skills;
using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Verification.Contracts;

namespace Agora.Agents.Contracts;

/// <summary>Everything a tool-capable agent backend needs to build an agent from config.</summary>
public sealed record AgentBuildContext
{
    public required AgentCard Card { get; init; }
    public required ModelSpec Spec { get; init; }
    public IReadOnlyList<Skill> Skills { get; init; } = Array.Empty<Skill>();
    public IReadOnlyList<string> Approvals { get; init; } = Array.Empty<string>();
    public IApprovalHandler? ApprovalHandler { get; init; }
    public IOutputInterpreter Interpreter { get; init; } = new SignalInterpreter();
    public McpConfig? Mcp { get; init; }

    /// <summary>Root directory the built-in filesystem tools are sandboxed to (read/write/search/list
    /// cannot escape it). Empty means the current working directory.</summary>
    public string FilesystemRoot { get; init; } = "";

    /// <summary>Shared knowledge base read access for the <c>rag_search</c> tool.</summary>
    public RagPipeline? Rag { get; init; }

    /// <summary>Shared knowledge base write access for the <c>rag_write</c> tool.</summary>
    public KnowledgeBase? KnowledgeBase { get; init; }

    /// <summary>Callback for the <c>ask_agent</c> tool: <c>(targetAgentId, question) =&gt; answer</c>.
    /// Null in answer-mode sub-calls so an interrogated agent cannot ask back (no recursion).</summary>
    public Func<string, string, Task<string>>? AskAgent { get; init; }

    /// <summary>Structured spec store for the <c>spec_*</c> tools, or null when SDD is disabled.</summary>
    public ISpecStore? SpecStore { get; init; }

    /// <summary>Whether the spec tools enforce "every requirement has an acceptance criterion".</summary>
    public bool SpecRequireCriteria { get; init; } = true;

    /// <summary>Runs allow-listed checks for the <c>run_check</c>/<c>spec_verify</c> tools, or null
    /// when no <c>checks</c> are configured.</summary>
    public ICheckRunner? CheckRunner { get; init; }
}

/// <summary>
/// The single seam between the framework-free core and a concrete agent backend (e.g.
/// <c>Agora.AgentFramework</c>). It bundles every config-driven construction the core delegates
/// outward: building tool-capable agents, and resolving the non-core embedders, vector stores, and
/// spec stores. A <c>TryCreate*</c> returning null means "the core already handles this type itself"
/// (e.g. the <c>fake</c> embedder, the <c>memory</c>/<c>file</c> stores). Tests implement only
/// <see cref="CreateToolAgent"/>; the resolvers default to core-handled.
/// </summary>
public interface IAgentBackend
{
    /// <summary>Builds a tool-capable agent (skills + MCP/function tools) from the build context.</summary>
    IAgent CreateToolAgent(AgentBuildContext context);

    /// <summary>Creates a non-core embedder for the spec, or null for core-handled types (<c>fake</c>).</summary>
    IEmbedder? TryCreateEmbedder(EmbedderSpec spec) => null;

    /// <summary>Creates a non-core vector store for the spec, or null for core-handled types
    /// (<c>memory</c>/<c>file</c>).</summary>
    IVectorStore? TryCreateVectorStore(VectorStoreConfig? spec) => null;

    /// <summary>Creates a non-core spec store for the resolved spec, or null for core-handled types
    /// (<c>file</c>).</summary>
    ISpecStore? TryCreateSpecStore(SpecStoreSpec spec) => null;
}
