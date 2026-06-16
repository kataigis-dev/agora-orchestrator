using Agora.AgentFramework;
using Agora.Cli;

return CliRunner.Run(
    args,
    new AgentFrameworkChatProvider(),
    toolAgentFactory: new AgentFrameworkToolAgentFactory(),
    approvalHandler: new ConsoleApprovalHandler());
