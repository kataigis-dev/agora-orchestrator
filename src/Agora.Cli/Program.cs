using Agora.AgentFramework;
using Agora.Cli;

return CliRunner.Run(
    args,
    new AgentFrameworkChatProvider(),
    toolAgentFactory: new AgentFrameworkToolAgentFactory(),
    approvalHandler: new ConsoleApprovalHandler(),
    conflictResolver: new ConsoleConflictResolver(),
    storeResolver: AgentFrameworkVectorStores.TryCreate,
    embedderResolver: AgentFrameworkEmbedders.TryCreate);
