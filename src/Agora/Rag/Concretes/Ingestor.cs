using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
namespace Agora.Rag.Concretes;

/// <summary>Reads text files, chunks them, embeds the chunks, and upserts to a store.</summary>
public sealed class Ingestor
{
    private static readonly string[] TextExtensions = { ".txt", ".md" };

    private readonly IEmbedder _embedder;
    private readonly IVectorStore _store;
    private readonly int _chunkSize;
    private readonly int _overlap;

    /// <summary>Creates the ingestor with its embedder, target store, and chunking parameters.</summary>
    public Ingestor(IEmbedder embedder, IVectorStore store, int chunkSize = 800, int overlap = 120)
    {
        _embedder = embedder;
        _store = store;
        _chunkSize = chunkSize;
        _overlap = overlap;
    }

    /// <summary>Reads every text file under the given paths, chunks and embeds them, upserts the
    /// result, and returns the number of chunks ingested.</summary>
    public async Task<int> IngestPathsAsync(
        IReadOnlyList<string> paths, CancellationToken cancellationToken = default)
    {
        var chunks = new List<Chunk>();
        foreach (var raw in paths)
        {
            foreach (var file in EnumerateTextFiles(raw))
            {
                var text = await File.ReadAllTextAsync(file, cancellationToken);
                foreach (var piece in TextChunker.Chunk(text, _chunkSize, _overlap))
                    chunks.Add(new Chunk(piece, file));
            }
        }
        if (chunks.Count == 0)
            return 0;

        var vectors = await _embedder.EmbedAsync(chunks.Select(c => c.Text).ToList(), cancellationToken);
        await _store.UpsertAsync(chunks, vectors, cancellationToken);
        return chunks.Count;
    }

    /// <summary>Yields supported text files (<c>.txt</c>/<c>.md</c>) for a file or directory path.</summary>
    private static IEnumerable<string> EnumerateTextFiles(string path)
    {
        if (File.Exists(path))
        {
            if (TextExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
                yield return path;
            yield break;
        }
        if (Directory.Exists(path))
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).OrderBy(f => f))
            {
                if (TextExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    yield return file;
            }
        }
    }
}
