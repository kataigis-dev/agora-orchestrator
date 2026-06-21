using Agora.Configuration;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace Agora.AgentFramework;

/// <summary>
/// Opens the configured MCP servers, exposes their tools (filtered to an agent's allowlist) as
/// <see cref="AITool"/>, and disposes the clients when the run completes. Connect per run with
/// <c>await using</c> so a stdio child process is never left dangling.
/// </summary>
internal sealed class McpToolSession : IAsyncDisposable
{
    private readonly IReadOnlyList<McpClient> _clients;

    /// <summary>The allow-listed tools exposed by the connected MCP servers.</summary>
    public IReadOnlyList<AITool> Tools { get; }

    private McpToolSession(IReadOnlyList<McpClient> clients, IReadOnlyList<AITool> tools)
    {
        _clients = clients;
        Tools = tools;
    }

    /// <summary>Connects to the configured MCP servers and collects the tools matching the agent's
    /// allow-list; returns an empty session when MCP is unconfigured or nothing is allow-listed.</summary>
    public static async Task<McpToolSession> ConnectAsync(
        McpConfig? mcp, IReadOnlyList<string> allow, CancellationToken cancellationToken)
    {
        var clients = new List<McpClient>();
        var tools = new List<AITool>();
        if (mcp is not null && allow.Count > 0)
        {
            var wanted = allow.ToHashSet(StringComparer.Ordinal);
            foreach (var (name, server) in mcp.Servers)
            {
                var client = await McpClient.CreateAsync(
                    BuildTransport(name, server), cancellationToken: cancellationToken);
                clients.Add(client);
                foreach (var tool in await client.ListToolsAsync(cancellationToken: cancellationToken))
                    if (wanted.Contains(tool.Name))
                        tools.Add(tool);
            }
        }
        return new McpToolSession(clients, tools);
    }

    /// <summary>Builds an HTTP or stdio transport for a server config.</summary>
    internal static IClientTransport BuildTransport(string name, McpServerConfig server) =>
        server.Url is not null
            ? new HttpClientTransport(new HttpClientTransportOptions
            {
                Name = name,
                Endpoint = new Uri(server.Url),
            })
            : new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = name,
                Command = server.Command
                    ?? throw new InvalidOperationException($"mcp server '{name}' has neither url nor command"),
                Arguments = server.Args,
            });

    /// <summary>Disposes all connected MCP clients (terminating any stdio child processes).</summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
            await client.DisposeAsync();
    }
}
