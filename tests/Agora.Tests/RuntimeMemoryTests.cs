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
}
