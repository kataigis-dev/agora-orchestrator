using System.Net;
using System.Net.Http.Json;

namespace Agora.Api.Tests;

public class RunLifecycleTests
{
    private static async Task<RunStatusResponse> PollUntil(
        HttpClient client, string runId, Func<RunStatusResponse, bool> done)
    {
        for (var i = 0; i < 200; i++)
        {
            var rec = await client.GetFromJsonAsync<RunStatusResponse>($"/runs/{runId}");
            if (rec is not null && done(rec))
                return rec;
            await Task.Delay(50);
        }
        throw new TimeoutException("run did not reach the expected state");
    }

    [Fact]
    public async Task AgentRun_CompletesWithOutput()
    {
        using var factory = new ApiFixture { Responses = () => new[] { "hello from agent" } };
        var client = factory.CreateClient();

        var start = await client.PostAsJsonAsync("/runs", new StartRunRequest("agent", "writer", "hi"));
        Assert.Equal(HttpStatusCode.Accepted, start.StatusCode);
        var runId = (await start.Content.ReadFromJsonAsync<StartRunResponse>())!.RunId;

        var rec = await PollUntil(client, runId, r => r.Status is "Completed" or "Failed");
        Assert.Equal("Completed", rec.Status);
        Assert.Equal("hello from agent", rec.Output);
    }

    [Fact]
    public async Task UnknownAgent_Returns400()
    {
        using var factory = new ApiFixture();
        var resp = await factory.CreateClient().PostAsJsonAsync("/runs", new StartRunRequest("agent", "ghost", "hi"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UnknownRun_Returns404()
    {
        using var factory = new ApiFixture();
        var resp = await factory.CreateClient().GetAsync("/runs/nope");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
