using System.Runtime.CompilerServices;
using Agora;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using Xunit;

namespace Agora.Tests.E2E;

public class RagRunE2ETests
{
    private static string ExamplesDir([CallerFilePath] string thisFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "examples"));

    [Fact]
    public async Task RagDemo_RetrievesAndFeedsContext_Offline()
    {
        var examples = ExamplesDir();
        var embedder = new FakeEmbedder(64);
        var store = new InMemoryVectorStore();
        var ingestor = new Ingestor(embedder, store, chunkSize: 200, overlap: 40);
        await ingestor.IngestPathsAsync(new[] { Path.Combine(examples, "knowledge") });
        var pipeline = new RagPipeline(new NoOpRefiner(), embedder, store, topK: 2, scoreThreshold: 0.0);

        var provider = new FakeChatProvider(new[] { "ANSWER" });
        var runtime = Runtime.FromConfig(Path.Combine(examples, "agora-rag.yaml"), provider, pipeline);

        var result = await runtime.RunAsync("What is Agora?");

        Assert.Equal("ANSWER", result.Output);
        Assert.NotNull(result.Enriched);
        Assert.NotEmpty(result.Enriched!.Retrieved);
        var entryUserMsg = provider.Calls[0].Messages[^1].Content;
        Assert.Contains("Agora", entryUserMsg);
    }
}
