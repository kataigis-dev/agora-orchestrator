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
}
