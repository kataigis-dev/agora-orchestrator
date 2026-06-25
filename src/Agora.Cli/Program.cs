using Agora.AgentFramework.Agents;
using Agora.AgentFramework.Mcp;
using Agora.AgentFramework.Providers;
using Agora.Cli;

using var provider = new AgentFrameworkChatProvider();
using var backend = new AgentFrameworkBackend();

return CliRunner.Run(
    args,
    provider,
    backend: backend,
    approvalHandler: new ConsoleApprovalHandler(),
    conflictResolver: new ConsoleConflictResolver(),
    mcpServer: new RagMcpStdioServer());
