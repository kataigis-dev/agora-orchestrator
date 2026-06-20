namespace Agora.Configuration;

public sealed class EmbedderConfig
{
    public string? Type { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
}

public sealed class VectorStoreConfig
{
    public string Type { get; set; } = "memory";
    public string? Path { get; set; }
    public string? Collection { get; set; }
    public string? Url { get; set; }
}

public sealed class RetrievalConfig
{
    public EmbedderConfig? Embedder { get; set; }
    public VectorStoreConfig? VectorStore { get; set; }
    public int TopK { get; set; } = 6;
    public double ScoreThreshold { get; set; }
}

public sealed class RefineConfig
{
    public string Strategy { get; set; } = "none";
    public string? Model { get; set; }
}

public sealed class IngestConfig
{
    public List<string> Sources { get; set; } = new();
    public int ChunkSize { get; set; } = 800;
    public int ChunkOverlap { get; set; } = 120;
}

public sealed class RagConfig
{
    public bool Enabled { get; set; } = true;
    public RefineConfig? Refine { get; set; }
    public RetrievalConfig? Retrieval { get; set; }
    public IngestConfig? Ingest { get; set; }
}
