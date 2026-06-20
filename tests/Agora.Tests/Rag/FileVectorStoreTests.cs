using Agora.Rag;
using Xunit;

namespace Agora.Tests.Rag;

public class FileVectorStoreTests
{
    private static string TempPath() => Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");

    [Fact]
    public void Persists_AcrossInstances()
    {
        var path = TempPath();
        try
        {
            var store = new FileVectorStore(path);
            store.Upsert(new[] { new Chunk("cat", "a") }, new[] { new float[] { 1, 0 } });

            var reloaded = new FileVectorStore(path);
            var hits = reloaded.Query(new float[] { 1, 0 }, topK: 1);
            Assert.Equal("cat", Assert.Single(hits).Text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void CreatesDirectory_WhenMissing()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var path = Path.Combine(dir, "kb.json");
        try
        {
            new FileVectorStore(path).Upsert(new[] { new Chunk("x", "s") }, new[] { new float[] { 1 } });
            Assert.True(File.Exists(path));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void MissingFile_LoadsEmpty()
    {
        var store = new FileVectorStore(TempPath());
        Assert.Empty(store.Query(new float[] { 1, 0 }, topK: 5));
    }
}
