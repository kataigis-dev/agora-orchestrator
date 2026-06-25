using Agora.Agents.Contracts;
using Agora.Configuration;
using Agora.Specs.Contracts;
using Agora.Specs.Models;

namespace Agora.Specs.Concretes;

/// <summary>Resolves the structured spec store from config — the core built-in <see cref="FileSpecStore"/>,
/// or a non-core store (e.g. RAG-over-MCP) via the injected <see cref="IAgentBackend"/>. The spec-store
/// counterpart to <c>RagFactory</c>: all config-driven resource construction lives in dedicated core
/// factories that delegate non-core types outward, so the runtime owns none of it. Null when SDD is
/// absent/disabled.</summary>
public static class SpecStoreFactory
{
    /// <summary>Builds the spec store for the config, resolving relative file paths against
    /// <paramref name="configDir"/> and non-core types through <paramref name="backend"/>.</summary>
    public static ISpecStore? Build(AgoraConfig config, string? configDir, IAgentBackend? backend)
    {
        if (config.Spec is not { Enabled: true } spec)
            return null;
        var store = spec.Store ?? new SpecStoreConfig();
        if (string.Equals(store.Type, "file", StringComparison.OrdinalIgnoreCase))
        {
            var path = store.Path ?? "spec.json";
            if (!Path.IsPathRooted(path))
                path = Path.Combine(configDir ?? Directory.GetCurrentDirectory(), path);
            return new FileSpecStore(path);
        }
        // Non-core stores (e.g. RAG over an MCP server) are built by the edge-injected backend resolver.
        var serverName = store.Server
            ?? throw new InvalidOperationException($"spec.store type '{store.Type}' requires a 'server'");
        McpServerConfig? server = null;
        config.Mcp?.Servers.TryGetValue(serverName, out server);
        var resolved = new SpecStoreSpec(
            store.Type, serverName, server, store.WriteTool, store.ReadTool, store.WriteArg, store.QueryArg, store.Key);
        return backend?.TryCreateSpecStore(resolved)
            ?? throw new NotSupportedException(
                $"spec store type '{store.Type}' has no built-in implementation and no backend provided");
    }
}
