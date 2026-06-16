using Agora.Agents;

namespace Agora.AgentFramework;

/// <summary>
/// Builds Microsoft Agent Framework-backed tool agents. Inject into the Runtime so agents that
/// declare skills/tools get a MAF agent while tool-less agents keep using the framework-free core.
/// </summary>
public sealed class AgentFrameworkToolAgentFactory : IToolAgentFactory
{
    public IAgent Create(AgentBuildContext context) => new AgentFrameworkAgent(context);
}
