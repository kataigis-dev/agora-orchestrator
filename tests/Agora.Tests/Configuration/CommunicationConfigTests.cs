using Agora.Configuration;
using Xunit;

namespace Agora.Tests.Configuration;

public class CommunicationConfigTests
{
    private static string Write(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, yaml);
        return path;
    }

    private const string Base = """
        providers:
          openai: { api_key_env: OPENAI_API_KEY }
        models:
          balanced: { provider: openai, model: gpt-4o }
        agents:
          writer: { model: balanced }
        """;

    [Fact]
    public void Default_IsH2c()
    {
        var cfg = ConfigLoader.Load(Write(Base));
        Assert.Equal("h2c", cfg.Communication);
    }

    [Fact]
    public void Natural_IsParsed()
    {
        var cfg = ConfigLoader.Load(Write(Base + "\ncommunication: natural\n"));
        Assert.Equal("natural", cfg.Communication);
    }

    [Fact]
    public void Unknown_Throws()
    {
        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Load(Write(Base + "\ncommunication: morse\n")));
        Assert.Contains("communication", ex.Message);
    }
}
