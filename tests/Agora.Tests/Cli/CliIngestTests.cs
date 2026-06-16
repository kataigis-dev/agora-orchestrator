using Agora.Cli;
using Agora.Providers;
using Xunit;

namespace Agora.Tests.Cli;

public class CliIngestTests
{
    [Fact]
    public void Ingest_ReportsChunkCount()
    {
        var kb = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(kb);
        File.WriteAllText(Path.Combine(kb, "doc.md"), "agora multi-agent framework");

        var sources = kb.Replace("\\", "/");
        var config = $$"""
            models: { balanced: { provider: openai, model: gpt-4o } }
            agents: { a: { model: balanced } }
            rag:
              enabled: true
              retrieval: { embedder: { type: fake }, vector_store: { type: memory } }
              ingest: { sources: [ "{{sources}}" ], chunk_size: 100, chunk_overlap: 0 }
            """;
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, config);

        var outw = new StringWriter();
        var code = CliRunner.Run(
            new[] { "ingest", "--config", path }, new FakeChatProvider(), outw, new StringWriter());

        Assert.Equal(0, code);
        Assert.Contains("ingested 1 chunks", outw.ToString());
    }
}
