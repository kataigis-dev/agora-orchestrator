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
using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Specs.Concretes;

namespace Agora.Cli;

/// <summary>Parsed CLI options bundled with the injected I/O streams and edge dependencies
/// (provider, tool factory, HITL handlers, and RAG/spec-store resolvers) that each command needs.</summary>
internal record ConfigState(Dictionary<string, string> Options, IChatProvider? Provider, IToolAgentFactory? ToolAgentFactory, IApprovalHandler? ApprovalHandler,
    IConflictResolver? ConflictResolver, Func<VectorStoreConfig?, IVectorStore?>? StoreResolver, Func<EmbedderSpec, IEmbedder?>? EmbedderResolver,
    Func<SpecStoreSpec, ISpecStore?>? SpecStoreResolver,
    TextReader In, TextWriter Out, TextWriter Error);