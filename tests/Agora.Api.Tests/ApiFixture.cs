using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using Agora.Configuration;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Agora.Api.Tests;

/// <summary>Spins up the API with an in-memory config and a FakeChatProvider (no network).</summary>
public sealed class ApiFixture : WebApplicationFactory<Program>
{
    public AgoraConfig Config { get; init; } = DefaultConfig();
    public IAgentBackend? Backend { get; init; }
    public Func<IEnumerable<string>>? Responses { get; init; }

    public static AgoraConfig DefaultConfig() => new()
    {
        Providers = { ["openai"] = new() { ApiKeyEnv = "OPENAI_API_KEY" } },
        Models = { ["balanced"] = new() { Provider = "openai", Model = "gpt-4o" } },
        Agents = { ["writer"] = new() { Model = "balanced", Role = "You write." } },
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AgoraConfig>();
            services.AddSingleton(Config);
            services.RemoveAll<IChatProvider>();
            services.AddSingleton<IChatProvider>(_ => new FakeChatProvider(Responses?.Invoke()));
            if (Backend is not null)
            {
                services.RemoveAll<IAgentBackend>();
                services.AddSingleton<IAgentBackend>(Backend);
            }
        });
    }
}
