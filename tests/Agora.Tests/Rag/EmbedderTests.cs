using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using Xunit;

namespace Agora.Tests.Rag;

public class EmbedderTests
{
    [Fact]
    public async Task FakeEmbedder_IsDeterministic_AndCorrectDim()
    {
        var emb = new FakeEmbedder(16);
        var v1 = await emb.EmbedAsync(new[] { "hello world" });
        var v2 = await emb.EmbedAsync(new[] { "hello world" });
        Assert.Single(v1);
        Assert.Equal(16, v1[0].Length);
        Assert.Equal(v1[0], v2[0]);
    }

    [Fact]
    public async Task FakeEmbedder_SharedWords_Overlap()
    {
        var emb = new FakeEmbedder(64);
        var va = (await emb.EmbedAsync(new[] { "cat dog" }))[0];
        var vb = (await emb.EmbedAsync(new[] { "dog bird" }))[0];
        var shared = Enumerable.Range(0, 64).Where(i => va[i] > 0 && vb[i] > 0).ToList();
        Assert.NotEmpty(shared);
    }
}
