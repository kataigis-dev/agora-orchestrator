using Agora.AgentFramework;
using Agora.Configuration;
using Agora.Rag;

namespace Agora.Tests.AgentFramework;

// Connection is lazy, so constructing a QdrantVectorStore touches no network — only the
// resolver wiring is unit-tested here; behaviour against a live Qdrant is verified separately.
public class QdrantVectorStoreTests
{
    [Fact]
    public void Resolver_CreatesQdrantStore_ForQdrantType()
    {
        var store = AgentFrameworkVectorStores.TryCreate(
            new VectorStoreConfig { Type = "qdrant", Url = "http://localhost:6334", Collection = "kb" });
        Assert.IsType<QdrantVectorStore>(store);
    }

    [Fact]
    public void Resolver_ReturnsNull_ForCoreTypes()
    {
        Assert.Null(AgentFrameworkVectorStores.TryCreate(new VectorStoreConfig { Type = "memory" }));
        Assert.Null(AgentFrameworkVectorStores.TryCreate(new VectorStoreConfig { Type = "file" }));
        Assert.Null(AgentFrameworkVectorStores.TryCreate(null));
    }

    [Fact]
    public void Qdrant_ImplementsIVectorStore()
        => Assert.IsAssignableFrom<IVectorStore>(
            new QdrantVectorStore("http://localhost:6334", "kb"));
}
