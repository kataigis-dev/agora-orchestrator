using Agora.Providers;
using Agora.Rag;
using Xunit;

namespace Agora.Tests.Rag;

public class RefinerTests
{
    private static ModelSpec Spec() => new()
    {
        Alias = "fast", Provider = "x", Model = "x", Temperature = 0, MaxTokens = 100, Timeout = 5, Retries = 0,
    };

    [Fact]
    public async Task NoOpRefiner_ReturnsInput()
    {
        var rq = await new NoOpRefiner().RefineAsync("what is rag?");
        Assert.Equal("what is rag?", rq.Query);
        Assert.Empty(rq.SubQueries);
    }

    [Fact]
    public async Task LlmRefiner_ParsesQueryAndSubqueries()
    {
        var provider = new FakeChatProvider(new[] { "retrieval augmented generation\nvector search\nembeddings" });
        var rq = await new LlmRefiner(provider, Spec()).RefineAsync("rag?");
        Assert.Equal("retrieval augmented generation", rq.Query);
        Assert.Equal(new[] { "vector search", "embeddings" }, rq.SubQueries);
    }

    [Fact]
    public async Task LlmRefiner_FallsBackToOriginal_OnEmpty()
    {
        var provider = new FakeChatProvider(new[] { "   " });
        var rq = await new LlmRefiner(provider, Spec()).RefineAsync("original");
        Assert.Equal("original", rq.Query);
    }
}
