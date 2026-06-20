using Agora.Rag;
using Xunit;

namespace Agora.Tests.Rag;

public class IngestTests
{
    [Fact]
    public void ChunkText_WindowsWithOverlap()
    {
        var chunks = TextChunker.Chunk("abcdefghij", chunkSize: 4, overlap: 1);
        Assert.Equal(new[] { "abcd", "defg", "ghij", "j" }, chunks);
    }

    [Fact]
    public void ChunkText_RejectsNonpositiveSize()
    {
        Assert.Throws<ArgumentException>(() => TextChunker.Chunk("x", chunkSize: 0, overlap: 0));
    }

    [Fact]
    public async Task Ingestor_ReadsFiles_AndPopulatesStore()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "a.md"), "cats are great");
        await File.WriteAllTextAsync(Path.Combine(dir, "b.txt"), "dogs are loyal");
        await File.WriteAllTextAsync(Path.Combine(dir, "ignore.png"), "binary");

        var embedder = new FakeEmbedder(64);
        var store = new InMemoryVectorStore();
        var ingestor = new Ingestor(embedder, store, chunkSize: 100, overlap: 0);
        var count = await ingestor.IngestPathsAsync(new[] { dir });

        Assert.Equal(2, count);
        var vec = (await embedder.EmbedAsync(new[] { "cats" }))[0];
        var hits = await store.QueryAsync(vec, topK: 1, scoreThreshold: 0.0);
        Assert.NotEmpty(hits);
        Assert.Contains("cats", hits[0].Text);
    }
}
