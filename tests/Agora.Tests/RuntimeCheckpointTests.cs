using Agora;
using Agora.Orchestration;
using Agora.Providers;
using Xunit;

namespace Agora.Tests;

public class RuntimeCheckpointTests
{
    private const string Config = """
        providers: { openai: { api_key_env: K } }
        models: { balanced: { provider: openai, model: gpt-4o } }
        agents:
          planner: { model: balanced, role: "Plan." }
          writer: { model: balanced, role: "Write." }
        graph:
          entry: planner
          edges:
            - { from: planner, to: writer, type: sequential }
            - { from: writer, to: END, type: sequential }
        """;

    [Fact]
    public async Task Run_PersistsCheckpoint_AndResumeReturnsResult()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var configPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(configPath, Config);
        try
        {
            var runtime = Runtime.FromConfig(configPath, new FakeChatProvider(new[] { "PLAN", "FINAL" }),
                checkpointStore: new FileCheckpointStore(dir));
            var result = await runtime.RunAsync("task", runId: "run42");

            Assert.Equal("run42", result.RunId);
            Assert.Equal("FINAL", result.Output);
            Assert.True(File.Exists(Path.Combine(dir, "run42.json")));

            // A fresh runtime resumes the (completed) run from disk and returns the final output.
            var resumed = await Runtime
                .FromConfig(configPath, new FakeChatProvider(), checkpointStore: new FileCheckpointStore(dir))
                .ResumeAsync("run42");
            Assert.Equal("FINAL", resumed.Output);
        }
        finally
        {
            File.Delete(configPath);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
