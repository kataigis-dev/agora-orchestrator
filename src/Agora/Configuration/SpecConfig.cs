namespace Agora.Configuration;

/// <summary>Selects where the structured specification is persisted.</summary>
public sealed class SpecStoreConfig
{
    /// <summary>Store type: <c>file</c> (canonical JSON on disk, the deterministic default) or
    /// <c>mcp</c> (a RAG/knowledge store reached over an MCP server).</summary>
    public string Type { get; set; } = "file";

    /// <summary>On-disk path for the <c>file</c> store; defaults to <c>spec.json</c> beside the config.</summary>
    public string? Path { get; set; }

    /// <summary>For <c>mcp</c>: the <c>mcp.servers</c> entry that backs the store.</summary>
    public string? Server { get; set; }

    /// <summary>For <c>mcp</c>: the tool that persists the spec payload.</summary>
    public string WriteTool { get; set; } = "rag_write";

    /// <summary>For <c>mcp</c>: the tool that retrieves the spec payload.</summary>
    public string ReadTool { get; set; } = "rag_search";

    /// <summary>For <c>mcp</c>: argument name carrying the payload on the write tool.</summary>
    public string WriteArg { get; set; } = "text";

    /// <summary>For <c>mcp</c>: argument name carrying the lookup key on the read tool.</summary>
    public string QueryArg { get; set; } = "query";

    /// <summary>For <c>mcp</c>: stable key/query identifying the spec document in the store.</summary>
    public string Key { get; set; } = "agora:spec-document";
}

/// <summary>Structured spec-driven-development configuration (opt-in). When present and enabled, the
/// runtime builds an <c>ISpecStore</c> and exposes the structured <c>spec_*</c> tools to agents that
/// allow-list them.</summary>
public sealed class SpecConfig
{
    /// <summary>Whether the structured spec store and tools are enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Where the spec is persisted (defaults to a <c>file</c> store at <c>spec.json</c>).</summary>
    public SpecStoreConfig? Store { get; set; }

    /// <summary>Require every requirement to carry at least one acceptance criterion (enforced on write).</summary>
    public bool RequireCriteria { get; set; } = true;
}
