using System.Net.Http.Json;
using Agora.Agents;
using Agora.Configuration;
using Agora.HumanInTheLoop;

namespace Agora.Api.Tests;

public class ApprovalFlowTests
{
    private sealed class ApprovingFactory : IToolAgentFactory
    {
        public IAgent Create(AgentBuildContext context) => new ApprovalAgent(context);

        private sealed class ApprovalAgent : IAgent
        {
            private readonly AgentBuildContext _ctx;
            public ApprovalAgent(AgentBuildContext ctx) => _ctx = ctx;

            public async Task<AgentResult> RunAsync(string userInput, string context = "")
            {
                var approved = await _ctx.ApprovalHandler!.RequestAsync(new ApprovalRequest
                {
                    AgentId = _ctx.Card.Id, FunctionName = "write_file", Arguments = "{\"path\":\"x\"}",
                });
                return new AgentResult { Output = approved ? "did it" : "refused" };
            }
        }
    }

    private static AgoraConfig ConfigWithApprovalAgent()
    {
        var cfg = ApiFixture.DefaultConfig();
        cfg.Agents["editor"] = new AgentConfig
        {
            Model = "balanced", Role = "edits", Tools = { "write_file" }, Approvals = { "write_file" },
        };
        return cfg;
    }

    private static async Task<RunStatusResponse> PollUntil(
        HttpClient client, string runId, Func<RunStatusResponse, bool> done)
    {
        for (var i = 0; i < 200; i++)
        {
            var rec = await client.GetFromJsonAsync<RunStatusResponse>($"/runs/{runId}");
            if (rec is not null && done(rec)) return rec;
            await Task.Delay(50);
        }
        throw new TimeoutException("run did not reach the expected state");
    }

    [Fact]
    public async Task Run_PausesForApproval_ThenResumesOnApprove()
    {
        using var factory = new ApiFixture { Config = ConfigWithApprovalAgent(), ToolAgentFactory = new ApprovingFactory() };
        var client = factory.CreateClient();

        var runId = (await (await client.PostAsJsonAsync("/runs", new StartRunRequest("agent", "editor", "go")))
            .Content.ReadFromJsonAsync<StartRunResponse>())!.RunId;

        var awaiting = await PollUntil(client, runId, r => r.Status == "AwaitingApproval");
        Assert.Single(awaiting.PendingApprovals);
        Assert.Equal("write_file", awaiting.PendingApprovals[0].FunctionName);

        var approvalId = awaiting.PendingApprovals[0].Id;
        var resp = await client.PostAsJsonAsync($"/runs/{runId}/approvals",
            new ApprovalsRequest(new[] { new ApprovalDecision(approvalId, true) }));
        resp.EnsureSuccessStatusCode();

        var done = await PollUntil(client, runId, r => r.Status == "Completed");
        Assert.Equal("did it", done.Output);
    }
}
