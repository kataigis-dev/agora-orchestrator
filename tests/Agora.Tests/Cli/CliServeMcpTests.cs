using Agora.Cli;
using Agora.Providers.Concretes;
using Xunit;

namespace Agora.Tests.Cli;

public class CliServeMcpTests
{
    private sealed class FakeMcpServer : IRagMcpServer
    {
        public Func<string, CancellationToken, Task<string>>? Search { get; private set; }

        public Task ServeAsync(
            Func<string, CancellationToken, Task<string>> search, CancellationToken cancellationToken = default)
        {
            Search = search;
            return Task.CompletedTask;
        }
    }

    private const string RagConfig = """
        providers: { openai: { api_key_env: K } }
        models: { m: { provider: openai, model: x } }
        agents: { answerer: { model: m, role: "answer" } }
        rag:
          enabled: true
          retrieval:
            embedder: { type: fake }
            vector_store: { type: memory }
        """;

    private const string NoRagConfig = """
        providers: { openai: { api_key_env: K } }
        models: { m: { provider: openai, model: x } }
        agents: { answerer: { model: m, role: "answer" } }
        """;

    private static string Write(string config)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, config);
        return path;
    }

    [Fact]
    public async Task ServeMcp_WiresRagSearch_BackedByTheReadPipeline()
    {
        var path = Write(RagConfig);
        var fake = new FakeMcpServer();
        try
        {
            var code = CliRunner.Run(
                new[] { "serve-mcp", "--config", path },
                new FakeChatProvider(), new StringWriter(), new StringWriter(), mcpServer: fake);

            Assert.Equal(0, code);
            Assert.NotNull(fake.Search);
            // The wired tool runs the read pipeline (returns context, never throws) — the same path the CLI uses.
            var result = await fake.Search!("anything", CancellationToken.None);
            Assert.NotNull(result);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ServeMcp_NoRagSection_ReturnsError()
    {
        var path = Write(NoRagConfig);
        var errw = new StringWriter();
        try
        {
            var code = CliRunner.Run(
                new[] { "serve-mcp", "--config", path },
                new FakeChatProvider(), new StringWriter(), errw, mcpServer: new FakeMcpServer());

            Assert.Equal(1, code);
            Assert.Contains("no enabled 'rag'", errw.ToString());
        }
        finally { File.Delete(path); }
    }
}
