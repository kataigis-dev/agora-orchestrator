namespace Agora.Configuration;

/// <summary>Selects the embedder used to vectorize text.</summary>
public sealed class EmbedderConfig
{
    /// <summary>Embedder type (<c>fake</c> built-in, or <c>openai</c>/<c>ollama</c> via the framework resolver).</summary>
    public string? Type { get; set; }

    /// <summary>Provider name backing a real embedder (resolves keys/base URL).</summary>
    public string? Provider { get; set; }

    /// <summary>Concrete embedding model name.</summary>
    public string? Model { get; set; }
}

/// <summary>Selects the vector store backing retrieval and the knowledge base.</summary>
public sealed class VectorStoreConfig
{
    /// <summary>Store type: <c>memory</c>, <c>file</c>, or <c>qdrant</c> (via the framework resolver).</summary>
    public string Type { get; set; } = "memory";

    /// <summary>On-disk path for the <c>file</c> store.</summary>
    public string? Path { get; set; }

    /// <summary>Collection name for a remote store such as Qdrant.</summary>
    public string? Collection { get; set; }

    /// <summary>Server URL for a remote store such as Qdrant.</summary>
    public string? Url { get; set; }
}

/// <summary>Retrieval settings: embedder, store, and query parameters.</summary>
public sealed class RetrievalConfig
{
    /// <summary>Default minimum similarity score for retrieved chunks.</summary>
    public const double DefaultScoreThreshold = 0.2;

    /// <summary>Embedder configuration.</summary>
    public EmbedderConfig? Embedder { get; set; }

    /// <summary>Vector store configuration.</summary>
    public VectorStoreConfig? VectorStore { get; set; }

    /// <summary>Number of chunks to retrieve per query.</summary>
    public int TopK { get; set; } = 6;

    /// <summary>Minimum similarity score a chunk must reach to be returned.</summary>
    public double ScoreThreshold { get; set; } = DefaultScoreThreshold;
}

/// <summary>Optional post-retrieval query/context refinement.</summary>
public sealed class RefineConfig
{
    /// <summary>Refine strategy: <c>none</c> or <c>llm</c>.</summary>
    public string Strategy { get; set; } = "none";

    /// <summary>Model alias used when the strategy is <c>llm</c>.</summary>
    public string? Model { get; set; }
}

/// <summary>Ingestion settings: sources and chunking.</summary>
public sealed class IngestConfig
{
    /// <summary>File or directory paths to ingest.</summary>
    public List<string> Sources { get; set; } = new();

    /// <summary>Target chunk size in characters.</summary>
    public int ChunkSize { get; set; } = 800;

    /// <summary>Overlap in characters between consecutive chunks.</summary>
    public int ChunkOverlap { get; set; } = 120;
}

/// <summary>RAG / shared knowledge-base configuration.</summary>
public sealed class RagConfig
{
    /// <summary>Whether RAG is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Optional refinement configuration.</summary>
    public RefineConfig? Refine { get; set; }

    /// <summary>Retrieval configuration (embedder, store, query params).</summary>
    public RetrievalConfig? Retrieval { get; set; }

    /// <summary>Ingestion configuration (sources, chunking).</summary>
    public IngestConfig? Ingest { get; set; }
}
