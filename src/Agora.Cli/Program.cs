using Agora.AgentFramework.Agents;
using Agora.AgentFramework.Providers;
using Agora.AgentFramework.Rag;
using Agora.AgentFramework.Specs;
using Agora.AgentFramework.Mcp;
using Agora.AgentFramework.Tools;
using Agora.Cli;

using var provider = new AgentFrameworkChatProvider();
using var toolAgentFactory = new AgentFrameworkToolAgentFactory();

return CliRunner.Run(
    args,
    provider,
    toolAgentFactory: toolAgentFactory,
    approvalHandler: new ConsoleApprovalHandler(),
    conflictResolver: new ConsoleConflictResolver(),
    storeResolver: AgentFrameworkVectorStores.TryCreate,
    embedderResolver: AgentFrameworkEmbedders.TryCreate,
    specStoreResolver: AgentFrameworkSpecStores.TryCreate);
