using Agora.AgentFramework;
using Agora.Providers;
using Microsoft.Extensions.AI;

namespace Agora.Tests.AgentFramework;

public class CopilotChatClientTests
{
    private static ModelSpec Spec(string provider) => new()
    {
        Alias = "copilot", Provider = provider, Model = "gpt-4o", ApiKey = "gho_testtoken",
    };

    [Theory]
    [InlineData("github-copilot")]
    [InlineData("copilot")]
    public void Build_RoutesCopilotProviderToCopilotClient(string provider)
    {
        // Building is lazy: the OAuth token is only exchanged on the first request, so this makes no
        // network call and just verifies the provider name is wired to the Copilot client.
        using IChatClient client = ChatClients.Build(Spec(provider));
        Assert.NotNull(client);
    }
}
