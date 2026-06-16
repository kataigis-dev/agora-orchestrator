using Agora.Rag;
using Xunit;

namespace Agora.Tests.Rag;

public class RagTypesTests
{
    [Fact]
    public void RefinedQuery_Defaults()
    {
        var rq = new RefinedQuery { Query = "cats" };
        Assert.Equal("cats", rq.Query);
        Assert.Empty(rq.SubQueries);
    }

    [Fact]
    public void EnrichedInput_AsContext_EmptyWhenNoHits()
    {
        var ei = new EnrichedInput { Original = "q", RefinedQuery = "q" };
        Assert.Empty(ei.Retrieved);
        Assert.Equal("", ei.AsContext());
    }

    [Fact]
    public void EnrichedInput_AsContext_FormatsHits()
    {
        var ei = new EnrichedInput
        {
            Original = "q",
            RefinedQuery = "q",
            Retrieved = new[] { new Chunk("alpha", "a.md", 0.9), new Chunk("beta", "b.md", 0.5) },
        };
        var ctx = ei.AsContext();
        Assert.Contains("Relevant context:", ctx);
        Assert.Contains("[1] (a.md) alpha", ctx);
        Assert.Contains("[2] (b.md) beta", ctx);
    }
}
