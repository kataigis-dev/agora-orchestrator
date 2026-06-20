using Agora.Agents;

namespace Agora.AgentFramework;

/// <summary>
/// Builds Microsoft Agent Framework-backed tool agents. Inject into the Runtime so agents that
/// declare skills/tools get a MAF agent while tool-less agents keep using the framework-free core.
/// Owns a <see cref="ChatClientCache"/> shared by every agent it builds and released on dispose.
/// </summary>
public sealed class AgentFrameworkToolAgentFactory : IToolAgentFactory, IDisposable
{
    private readonly ChatClientCache _clients = new();

    /// <inheritdoc />
    public IAgent Create(AgentBuildContext context) => new AgentFrameworkAgent(context, _clients);

    /// <summary>Disposes the shared chat-client cache.</summary>
    public void Dispose() => _clients.Dispose();
}
