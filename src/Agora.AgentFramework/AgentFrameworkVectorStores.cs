using Agora.Configuration;
using Agora.Rag;

namespace Agora.AgentFramework;

/// <summary>
/// Resolves non-core <see cref="IVectorStore"/> implementations from config, so the
/// framework-free core can stay unaware of them. Injected into the Runtime/CLI as a store
/// resolver; returns null for types the core already handles (memory/file).
/// </summary>
public static class AgentFrameworkVectorStores
{
    /// <summary>Creates a <c>qdrant</c> store from the spec, or null for core-handled types.</summary>
    public static IVectorStore? TryCreate(VectorStoreConfig? spec) => spec?.Type switch
    {
        "qdrant" => new QdrantVectorStore(spec.Url ?? "http://localhost:6334", spec.Collection ?? "agora"),
        _ => null,
    };
}
