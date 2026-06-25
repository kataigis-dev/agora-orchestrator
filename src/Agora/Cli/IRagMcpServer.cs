namespace Agora.Cli;

/// <summary>
/// Serves a single, read-only <c>rag_search</c> tool over an MCP stdio transport. The framework-free core
/// supplies only the search function; the concrete server (using the MCP server SDK) is injected at the
/// edge — the same edge dependency-injection pattern as the chat provider and agent backend. The server
/// exposes no write/mutation tool and opens no network port, so <c>rag_write</c> never leaves the CLI +
/// human path.
/// </summary>
public interface IRagMcpServer
{
    /// <summary>Runs the stdio MCP server, exposing one read-only <c>rag_search</c> tool backed by
    /// <paramref name="search"/> (query → formatted context), until the transport closes or the token is
    /// cancelled.</summary>
    Task ServeAsync(
        Func<string, CancellationToken, Task<string>> search, CancellationToken cancellationToken = default);
}
