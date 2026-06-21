using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Specs.Concretes;
using Agora.Configuration;

namespace Agora.Specs.Models;

/// <summary>
/// A resolved request to build a non-core spec store, handed to the edge-injected resolver (mirrors
/// the RAG vector-store/embedder resolvers). Carries the MCP server it should talk to plus the tool
/// and argument names used to persist/retrieve the spec payload, so the framework-free core never
/// references an MCP client.
/// </summary>
public sealed record SpecStoreSpec(
    string Type,
    string ServerName,
    McpServerConfig? Server,
    string WriteTool,
    string ReadTool,
    string WriteArg,
    string QueryArg,
    string Key);
