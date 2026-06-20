using Agora.AgentFramework;
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
    embedderResolver: AgentFrameworkEmbedders.TryCreate);
