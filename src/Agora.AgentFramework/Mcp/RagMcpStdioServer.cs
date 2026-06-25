using Agora.Cli;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Agora.AgentFramework.Mcp;

/// <summary>
/// A local, read-only MCP <b>stdio</b> server exposing a single <c>rag_search</c> tool over the knowledge
/// base. It is a child process the client launches over standard in/out — no network port is opened — and
/// it exposes no write/mutation tool, so <c>rag_write</c> never leaves the CLI + human path. Implements the
/// core's <see cref="IRagMcpServer"/> seam using the MCP server SDK (which lives outside the framework-free
/// core).
/// </summary>
public sealed class RagMcpStdioServer : IRagMcpServer
{
    /// <inheritdoc />
    public async Task ServeAsync(
        Func<string, CancellationToken, Task<string>> search, CancellationToken cancellationToken = default)
    {
        var options = new McpServerOptions
        {
            ServerInfo = new Implementation { Name = "agora-rag", Version = "0.1.0" },
            ToolCollection = BuildTools(search),
        };
        await using var transport = new StdioServerTransport(options);
        await using var server = McpServer.Create(transport, options);
        await server.RunAsync(cancellationToken);
    }

    /// <summary>Builds the server's tool set: exactly one read-only <c>rag_search</c> tool. Exposed so a
    /// test can assert no write/mutation tool is present.</summary>
    public static McpServerPrimitiveCollection<McpServerTool> BuildTools(
        Func<string, CancellationToken, Task<string>> search)
    {
        var tools = new McpServerPrimitiveCollection<McpServerTool>(StringComparer.Ordinal);
        tools.Add(McpServerTool.Create(
            (string query, CancellationToken ct) => search(query, ct),
            new McpServerToolCreateOptions
            {
                Name = "rag_search",
                Description = "Search the read-only knowledge base and return the most relevant context.",
            }));
        return tools;
    }
}
