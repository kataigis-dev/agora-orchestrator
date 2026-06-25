using Agora;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Xunit;

namespace Agora.Tests;

public class RuntimeMemoryTests
{
    private const string Config = """
        communication: natural
        memory: { enabled: true, top_k: 5 }
        providers:
          openai: { api_key_env: OPENAI_API_KEY }
        models:
          balanced: { provider: openai, model: gpt-4o }
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
    public async Task MemoryEnabled_RecallsDeclaredArtifacts_AcrossAgents()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, Config);
        var provider = new FakeChatProvider(new[] { "planning <<artifact plan=use JWT auth>>", "FINAL" });

        var rt = Runtime.FromConfig(path, provider);
        await rt.RunAsync("build login");

        // The writer (2nd call) receives the planner's declared artifact recalled from memory.
        var writerMsg = provider.Calls[1].Messages[^1].Content;
        Assert.Contains("plan: use JWT auth", writerMsg);
        Assert.Contains("Relevant context:", writerMsg);
        File.Delete(path);
    }

    [Fact]
    public async Task TwoRuns_DoNotShareMemory()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, Config);
        // Run 1 declares an artifact; run 2's planner declares none. With per-run memory, run 2's
        // writer recalls an empty memory and must not see run 1's artifact.
        var provider = new FakeChatProvider(new[]
        {
            "planning <<artifact plan=use JWT auth>>", "FINAL",
            "second run, no artifact", "FINAL2",
        });

        var rt = Runtime.FromConfig(path, provider);
        await rt.RunAsync("build login");
        await rt.RunAsync("build dashboard");

        // Run 2's writer is the 4th call; its recalled context must not leak run 1's artifact.
        var run2WriterMsg = provider.Calls[3].Messages[^1].Content;
        Assert.DoesNotContain("plan: use JWT auth", run2WriterMsg);
        File.Delete(path);
    }
}
