using Agora.Configuration;
using Agora.Specs;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Agora.AgentFramework;

/// <summary>Calls a single MCP tool and returns its text content. Abstracted so the spec store can be
/// unit-tested without a live MCP server.</summary>
internal interface IMcpInvoker
{
    Task<string> CallAsync(string tool, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken);
}

/// <summary>
/// An <see cref="ISpecStore"/> backed by a RAG / knowledge store reached over MCP: the canonical spec
/// JSON is persisted via a write tool under a stable key and retrieved via a read/search tool. The
/// store stays the deterministic representation (<see cref="SpecSerializer"/>); the MCP server only
/// provides durable, possibly shared, storage. Because retrieval may be semantic, the payload is
/// tagged with the key and the JSON object is extracted from whatever surrounding text the tool
/// returns. For strict guarantees prefer the file store; this enables an external/shared spec.
/// </summary>
internal sealed class McpSpecStore : ISpecStore
{
    private readonly IMcpInvoker _invoker;
    private readonly SpecStoreSpec _spec;

    public McpSpecStore(IMcpInvoker invoker, SpecStoreSpec spec)
    {
        _invoker = invoker;
        _spec = spec;
    }

    /// <inheritdoc />
    public async Task<SpecDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _invoker.CallAsync(
            _spec.ReadTool,
            new Dictionary<string, object?> { [_spec.QueryArg] = _spec.Key },
            cancellationToken);
        var json = ExtractJsonObject(raw);
        return json is null ? SpecDocument.Empty : SpecSerializer.Deserialize(json);
    }

    /// <inheritdoc />
    public Task SaveAsync(SpecDocument document, CancellationToken cancellationToken = default)
    {
        // Tag the payload with the key so a semantic read tool can retrieve it later.
        var payload = _spec.Key + "\n" + SpecSerializer.Serialize(document);
        return _invoker.CallAsync(
            _spec.WriteTool,
            new Dictionary<string, object?> { [_spec.WriteArg] = payload },
            cancellationToken);
    }

    /// <summary>Extracts the outermost JSON object from tool output that may include surrounding
    /// context (labels, the key tag, multiple chunks). Returns null when none is present.</summary>
    private static string? ExtractJsonObject(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text.Substring(start, end - start + 1) : null;
    }
}

/// <summary>Real MCP invoker: connects to the configured server per call (connect → call → dispose,
/// like <see cref="McpToolSession"/>) so no stdio child process is left dangling. Spec writes/reads
/// are infrequent, so per-call connection is acceptable.</summary>
internal sealed class McpClientInvoker : IMcpInvoker
{
    private readonly string _serverName;
    private readonly McpServerConfig _server;

    public McpClientInvoker(string serverName, McpServerConfig server)
    {
        _serverName = serverName;
        _server = server;
    }

    public async Task<string> CallAsync(
        string tool, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        await using var client = await McpClient.CreateAsync(
            McpToolSession.BuildTransport(_serverName, _server), cancellationToken: cancellationToken);
        var result = await client.CallToolAsync(tool, arguments, cancellationToken: cancellationToken);
        return string.Join("\n", result.Content.OfType<TextContentBlock>().Select(b => b.Text));
    }
}
