using Agora.AgentFramework.Agents;
using Agora.AgentFramework.Providers;
using Agora.AgentFramework.Rag;
using Agora.AgentFramework.Specs;
using Agora.AgentFramework.Mcp;
using Agora.AgentFramework.Tools;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;

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
