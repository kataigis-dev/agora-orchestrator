using System.Net;
using System.Net.Http.Json;

namespace Agora.Api.Tests;

public class HealthAndAgentsTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _factory;
    public HealthAndAgentsTests(ApiFixture factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var resp = await _factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Agents_ListsConfiguredAgents()
    {
        var agents = await _factory.CreateClient().GetFromJsonAsync<List<AgentInfo>>("/agents");
        Assert.NotNull(agents);
        Assert.Contains(agents!, a => a.Id == "writer" && a.Role == "You write.");
    }
}
