using System.Net;
using System.Net.Http.Json;
using Agora.Configuration;

namespace Agora.Api.Tests;

public class IngestTests
{
    [Fact]
    public async Task Ingest_NoRag_Returns400()
    {
        using var factory = new ApiFixture(); // DefaultConfig has no rag
        var resp = await factory.CreateClient().PostAsync("/ingest", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Ingest_WithRag_ReturnsChunkCount()
    {
        var dir = Path.Combine(Path.GetTempPath(), "agora-api-ingest-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "doc.md"), "alpha beta gamma. delta epsilon zeta.");

        var cfg = ApiFixture.DefaultConfig();
        cfg.Rag = new RagConfig
        {
            Enabled = true,
            Ingest = new IngestConfig { Sources = { dir }, ChunkSize = 20, ChunkOverlap = 0 },
        };

        using var factory = new ApiFixture { Config = cfg };
        var resp = await factory.CreateClient().PostAsync("/ingest", content: null);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<IngestResponse>();
        Assert.True(body!.Chunks > 0);

        Directory.Delete(dir, recursive: true);
    }
}
