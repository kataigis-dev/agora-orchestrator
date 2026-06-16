using Agora.Configuration;
using Xunit;

namespace Agora.Tests.Configuration;

public class ApprovalConfigTests
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
        """;

    [Fact]
    public void Load_ParsesApprovalsList()
    {
        var cfg = ConfigLoader.Load(Write(Base + """

            agents:
              writer:
                model: balanced
                tools: [read_file, write_file]
                approvals: [write_file]
            """));
        Assert.Equal(new[] { "write_file" }, cfg.Agents["writer"].Approvals);
    }

    [Fact]
    public void Load_ApprovalNotInTools_Throws()
    {
        var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Load(Write(Base + """

            agents:
              writer:
                model: balanced
                tools: [read_file]
                approvals: [write_file]
            """)));
        Assert.Contains("approval", ex.Message);
    }

    [Fact]
    public void Load_NoApprovals_IsEmpty()
    {
        var cfg = ConfigLoader.Load(Write(Base + "\nagents:\n  writer: { model: balanced }\n"));
        Assert.Empty(cfg.Agents["writer"].Approvals);
    }
}
