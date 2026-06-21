using System.Runtime.CompilerServices;
using Agora;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Xunit;

namespace Agora.Tests.E2E;

public class GraphRunE2ETests
{
    // Resolves csharp/examples/agora-graph.yaml relative to this source file.
    private static string ExamplePath([CallerFilePath] string thisFile = "")
        => Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(thisFile)!, "..", "..", "..", "examples", "agora-graph.yaml"));

    [Fact]
    public async Task ExampleGraph_RunsWithRevisionLoop_Offline()
    {
        // planner -> writer(DRAFT1) -> critic(needs_revision) -> writer(DRAFT2)
        //         -> critic(approved) -> END
        var provider = new FakeChatProvider(new[]
        {
            "PLAN", "DRAFT1", "<<signal needs_revision>>", "DRAFT2", "Looks good. <<signal approved>>",
        });
        var runtime = Runtime.FromConfig(ExamplePath(), provider);

        var result = await runtime.RunAsync("Write a short note about agents");

        Assert.Equal("Looks good.", result.Output);
        Assert.Equal(1, result.State.LoopCounters["critic->writer"]);
        Assert.Equal("DRAFT2", result.State.Outputs["writer"]);
        Assert.Equal("PLAN", result.State.Outputs["planner"]);
    }
}
