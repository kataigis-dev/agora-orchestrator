using Agora.AgentFramework.Rag;
using Agora.AgentFramework.Specs;
using Agora.AgentFramework.Providers;
using Agora.Agents.Contracts;
using Agora.Configuration;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Specs.Contracts;
using Agora.Specs.Models;

namespace Agora.AgentFramework.Agents;

/// <summary>
/// The one adapter that satisfies <see cref="IAgentBackend"/> for the Microsoft Agent Framework. It
/// builds MAF-backed tool agents (so tool-less agents keep using the framework-free core) and resolves
/// the non-core embedders, vector stores, and spec stores the core can't build itself. Owns a
/// <see cref="ChatClientCache"/> shared by every agent it builds and released on dispose.
/// </summary>
public sealed class AgentFrameworkBackend : IAgentBackend, IDisposable
{
    private readonly ChatClientCache _clients = new();

    /// <inheritdoc />
    public IAgent CreateToolAgent(AgentBuildContext context) => new AgentFrameworkAgent(context, _clients);

    /// <inheritdoc />
    public IEmbedder? TryCreateEmbedder(EmbedderSpec spec) => AgentFrameworkEmbedders.TryCreate(spec);

    /// <inheritdoc />
    public IVectorStore? TryCreateVectorStore(VectorStoreConfig? spec) => AgentFrameworkVectorStores.TryCreate(spec);

    /// <inheritdoc />
    public ISpecStore? TryCreateSpecStore(SpecStoreSpec spec) => AgentFrameworkSpecStores.TryCreate(spec);

    /// <summary>Disposes the shared chat-client cache.</summary>
    public void Dispose() => _clients.Dispose();
}
