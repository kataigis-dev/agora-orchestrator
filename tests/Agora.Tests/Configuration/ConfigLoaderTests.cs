using Agora.Configuration;
using Xunit;

namespace Agora.Tests.Configuration;

public class ConfigLoaderTests
{
    private static string Write(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, yaml);
        return path;
    }

    private const string Valid = """
        version: "1"
        providers:
          anthropic: { api_key_env: ANTHROPIC_API_KEY }
        models:
          balanced: { provider: anthropic, model: claude-sonnet-4-6 }
        agents:
          writer: { model: balanced }
        """;

    [Fact]
    public void Load_ValidConfig_PopulatesModelAndDefaults()
    {
        var cfg = ConfigLoader.Load(Write(Valid));
        Assert.Equal("1", cfg.Version);
        Assert.Equal(0.2, cfg.Defaults.Temperature);
        Assert.Equal(4096, cfg.Defaults.MaxTokens);
        Assert.Equal("anthropic", cfg.Models["balanced"].Provider);
        Assert.Equal("balanced", cfg.Agents["writer"].Model);
    }

    [Fact]
    public void Load_IgnoresUnknownSections()
    {
        var cfg = ConfigLoader.Load(Write(Valid + "\nrag: { enabled: true }\ngraph: { entry: writer }\n"));
        Assert.Equal("balanced", cfg.Agents["writer"].Model);
    }

    [Fact]
    public void Load_MissingFile_Throws()
    {
        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Load("does-not-exist.yaml"));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void Load_UnknownModelAlias_Throws()
    {
        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Load(Write(
            "models: { balanced: { provider: anthropic, model: x } }\nagents: { writer: { model: missing } }\n")));
        Assert.Contains("unknown model alias", ex.Message);
    }

    [Fact]
    public void Load_NoAgents_Throws()
    {
        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Load(Write(
            "models: { balanced: { provider: anthropic, model: x } }\n")));
        Assert.Contains("at least one agent", ex.Message);
    }
}
