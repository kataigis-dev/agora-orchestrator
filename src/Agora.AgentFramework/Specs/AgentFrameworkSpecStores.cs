using Agora.AgentFramework.Tools;
using Agora.AgentFramework.Mcp;
using Agora.AgentFramework.Specs;
using Agora.AgentFramework.Rag;
using Agora.AgentFramework.Providers;
using Agora.AgentFramework.Agents;
using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Specs.Concretes;

namespace Agora.AgentFramework.Specs;

/// <summary>
/// Resolves non-core <see cref="ISpecStore"/> implementations from config, so the framework-free core
/// stays unaware of MCP. Inject as the runtime's <c>specStoreResolver</c> (mirrors
/// <see cref="AgentFrameworkVectorStores"/>). The core handles the built-in <c>file</c> store itself.
/// </summary>
public static class AgentFrameworkSpecStores
{
    /// <summary>Builds the store for a resolved spec, or null when the type is unknown.</summary>
    public static ISpecStore? TryCreate(SpecStoreSpec spec) => spec.Type switch
    {
        "mcp" => new McpSpecStore(
            new McpClientInvoker(
                spec.ServerName,
                spec.Server ?? throw new InvalidOperationException(
                    $"spec store server '{spec.ServerName}' is not defined under mcp.servers")),
            spec),
        _ => null,
    };
}
