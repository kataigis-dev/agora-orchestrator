using Agora.Agents.Models;
using Agora.Orchestration.Models;
using Xunit;

namespace Agora.Tests.Orchestration;

public class RunMetricsForAgentTests
{
    [Fact]
    public void ForAgent_ProjectsAgentResultIntoOneStepMetrics()
    {
        var result = new AgentResult
        {
            Output = "done",
            InputTokens = 120,
            OutputTokens = 40,
            CacheReadTokens = 90,
            CacheWriteTokens = 5,
            Signals = new() { ["pass"] = true },
        };

        var metrics = RunMetrics.ForAgent(result, TimeSpan.FromSeconds(2), "writer");

        Assert.Equal(1, metrics.Steps);
        Assert.True(metrics.Completed);
        Assert.Equal(0, metrics.ReworkCount);
        Assert.Equal(0, metrics.ParallelForks);
        Assert.Equal(TimeSpan.FromSeconds(2), metrics.Duration);
        Assert.Equal(1, metrics.NodeVisits["writer"]);
        Assert.Equal(1, metrics.Signals["pass"]);
        Assert.Equal(120, metrics.InputTokens);
        Assert.Equal(40, metrics.OutputTokens);
        Assert.Equal(90, metrics.CacheReadTokens);
        Assert.Equal(5, metrics.CacheWriteTokens);
        Assert.Equal(0.75, metrics.CacheHitRate);   // 90 / 120
    }
}
