using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using Xunit;

namespace Agora.Tests.Rag;

public class FileVectorStoreTests
{
    private static string TempPath() => Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");

    [Fact]
    public async Task Persists_AcrossInstances()
    {
        var path = TempPath();
        try
        {
            var store = new FileVectorStore(path);
            await store.UpsertAsync(new[] { new Chunk("cat", "a") }, new[] { new float[] { 1, 0 } });

            var reloaded = new FileVectorStore(path);
            var hits = await reloaded.QueryAsync(new float[] { 1, 0 }, topK: 1);
            Assert.Equal("cat", Assert.Single(hits).Text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task CreatesDirectory_WhenMissing()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var path = Path.Combine(dir, "kb.json");
        try
        {
            await new FileVectorStore(path).UpsertAsync(new[] { new Chunk("x", "s") }, new[] { new float[] { 1 } });
            Assert.True(File.Exists(path));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task MissingFile_LoadsEmpty()
    {
        var store = new FileVectorStore(TempPath());
        Assert.Empty(await store.QueryAsync(new float[] { 1, 0 }, topK: 5));
    }

    [Fact]
    public async Task ConcurrentUpserts_DoNotLoseEntries()
    {
        var path = TempPath();
        try
        {
            var store = new FileVectorStore(path);

            await Task.WhenAll(Enumerable.Range(0, 30).Select(i =>
                store.UpsertAsync(new[] { new Chunk($"fact {i}", "a") }, new[] { new float[] { i + 1, 1 } })));

            // All 30 survive the concurrent whole-file rewrites, in memory and on disk (the file reloads
            // cleanly, proving the atomic temp+move never left a corrupt or partial JSON).
            Assert.Equal(30, (await store.QueryAsync(new float[] { 1, 1 }, topK: 100, scoreThreshold: -1)).Count);
            var reloaded = new FileVectorStore(path);
            Assert.Equal(30, (await reloaded.QueryAsync(new float[] { 1, 1 }, topK: 100, scoreThreshold: -1)).Count);
            Assert.False(File.Exists(path + ".tmp")); // the atomic move leaves no temp file behind
        }
        finally { File.Delete(path); File.Delete(path + ".tmp"); }
    }
}
