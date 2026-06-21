using Agora.Agents;
using Agora.Configuration;
using Agora.HumanInTheLoop;
using Agora.Providers;
using Agora.Rag;
using Agora.Specs;

namespace Agora.Cli;

/// <summary>Parsed CLI options bundled with the injected I/O streams and edge dependencies
/// (provider, tool factory, HITL handlers, and RAG/spec-store resolvers) that each command needs.</summary>
internal record ConfigState(Dictionary<string, string> Options, IChatProvider? Provider, IToolAgentFactory? ToolAgentFactory, IApprovalHandler? ApprovalHandler,
    IConflictResolver? ConflictResolver, Func<VectorStoreConfig?, IVectorStore?>? StoreResolver, Func<EmbedderSpec, IEmbedder?>? EmbedderResolver,
    Func<SpecStoreSpec, ISpecStore?>? SpecStoreResolver,
    TextReader In, TextWriter Out, TextWriter Error);