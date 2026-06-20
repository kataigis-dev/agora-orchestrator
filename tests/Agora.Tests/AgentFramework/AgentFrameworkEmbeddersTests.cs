using Agora.AgentFramework;
using Agora.Rag;

namespace Agora.Tests.AgentFramework;

// Construction is lazy (no network), so only the resolver wiring is unit-tested here.
public class AgentFrameworkEmbeddersTests
{
    [Fact]
    public void TryCreate_OpenAI_ReturnsEmbedder()
        => Assert.IsType<AgentFrameworkEmbedder>(
            AgentFrameworkEmbedders.TryCreate(new EmbedderSpec("openai", "openai", "text-embedding-3-small", "k", null)));

    [Fact]
    public void TryCreate_Ollama_ReturnsEmbedder()
        => Assert.IsType<AgentFrameworkEmbedder>(
            AgentFrameworkEmbedders.TryCreate(new EmbedderSpec("ollama", "ollama", "nomic-embed-text", null, null)));

    [Fact]
    public void TryCreate_Fake_ReturnsNull()
        => Assert.Null(AgentFrameworkEmbedders.TryCreate(new EmbedderSpec("fake", "", "", null, null)));
}
