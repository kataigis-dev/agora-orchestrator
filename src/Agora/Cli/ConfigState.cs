using Agora.Agents;
using Agora.Configuration;
using Agora.HumanInTheLoop;
using Agora.Providers;
using Agora.Rag;

namespace Agora.Cli;

internal record ConfigState(Dictionary<string, string> Options, IChatProvider? Provider, IToolAgentFactory? ToolAgentFactory, IApprovalHandler? ApprovalHandler, 
    IConflictResolver? ConflictResolver, Func<VectorStoreConfig?, IVectorStore?>? StoreResolver, Func<EmbedderSpec, IEmbedder?>? EmbedderResolver);