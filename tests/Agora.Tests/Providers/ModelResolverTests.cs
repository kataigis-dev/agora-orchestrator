using Agora.Configuration;
using Agora.Providers;
using Xunit;

namespace Agora.Tests.Providers;

public class ModelResolverTests
{
    private static AgoraConfig Config() => new()
    {
        Defaults = new Defaults { Temperature = 0.5, MaxTokens = 1000, Timeout = 30, Retries = 1 },
        Providers = new()
        {
            ["anthropic"] = new ProviderConfig { ApiKeyEnv = "ANTHROPIC_API_KEY" },
            ["ollama"] = new ProviderConfig { BaseUrl = "http://localhost:11434" },
        },
        Models = new()
        {
            ["balanced"] = new ModelConfig { Provider = "anthropic", Model = "claude-sonnet-4-6" },
            ["local"] = new ModelConfig { Provider = "ollama", Model = "llama3.1:70b" },
        },
        Agents = new() { ["a"] = new AgentConfig { Model = "balanced" } },
    };

    [Fact]
    public void Resolve_AppliesDefaultsAndCopiesProviderModel()
    {
        var specs = ModelResolver.Resolve(Config());
        Assert.Equal("anthropic", specs["balanced"].Provider);
        Assert.Equal("claude-sonnet-4-6", specs["balanced"].Model);
        Assert.Equal(0.5, specs["balanced"].Temperature);
        Assert.Equal(1000, specs["balanced"].MaxTokens);
        Assert.Equal(30, specs["balanced"].Timeout);
        Assert.Equal(1, specs["balanced"].Retries);
    }

    [Fact]
    public void Resolve_ReadsApiKeyFromEnv_AndBaseUrl()
    {
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "sk-test-123");
        try
        {
            var specs = ModelResolver.Resolve(Config());
            Assert.Equal("sk-test-123", specs["balanced"].ApiKey);
            Assert.Equal("http://localhost:11434", specs["local"].ApiBase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
        }
    }

    [Fact]
    public void Resolve_PopulatesRetryBaseDelayFromDefaults()
    {
        var cfg = new Agora.Configuration.AgoraConfig
        {
            Defaults = new Agora.Configuration.Defaults { RetryBaseDelay = 0.25 },
            Providers = { ["openai"] = new() { ApiKeyEnv = "X" } },
            Models = { ["balanced"] = new() { Provider = "openai", Model = "gpt-4o" } },
        };
        var specs = ModelResolver.Resolve(cfg);
        Assert.Equal(0.25, specs["balanced"].RetryBaseDelay);
    }
}
