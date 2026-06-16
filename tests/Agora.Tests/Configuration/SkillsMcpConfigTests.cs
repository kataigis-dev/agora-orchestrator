using Agora.Configuration;
using Xunit;

namespace Agora.Tests.Configuration;

public class SkillsMcpConfigTests
{
    private static AgoraConfig Load(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, yaml);
        return ConfigLoader.Load(path);
    }

    private const string Base = """
        providers:
          openai: { api_key_env: OPENAI_API_KEY }
        models:
          balanced: { provider: openai, model: gpt-4o }
        """;

    [Fact]
    public void Load_ParsesAgentSkillsAndTools()
    {
        var cfg = Load(Base + """

            agents:
              writer:
                model: balanced
                skills: [summarize, outline]
                tools: [search]
            """);

        var writer = cfg.Agents["writer"];
        Assert.Equal(new[] { "summarize", "outline" }, writer.Skills);
        Assert.Equal(new[] { "search" }, writer.Tools);
    }

    [Fact]
    public void Load_AgentWithoutSkillsOrTools_HasEmptyLists()
    {
        var cfg = Load(Base + "\nagents:\n  writer: { model: balanced }\n");

        Assert.Empty(cfg.Agents["writer"].Skills);
        Assert.Empty(cfg.Agents["writer"].Tools);
    }

    [Fact]
    public void Load_ParsesSkillsDirectories()
    {
        var cfg = Load(Base + """

            agents:
              writer: { model: balanced }
            skills:
              directories: [./skills, /opt/shared-skills]
            """);

        Assert.NotNull(cfg.Skills);
        Assert.Equal(new[] { "./skills", "/opt/shared-skills" }, cfg.Skills!.Directories);
    }

    [Fact]
    public void Load_ParsesMcpStdioAndHttpServers()
    {
        var cfg = Load(Base + """

            agents:
              writer: { model: balanced }
            mcp:
              servers:
                filesystem:
                  command: npx
                  args: ["-y", "@modelcontextprotocol/server-filesystem", "/data"]
                remote:
                  url: https://example.com/mcp
            """);

        Assert.NotNull(cfg.Mcp);
        var fs = cfg.Mcp!.Servers["filesystem"];
        Assert.Equal("npx", fs.Command);
        Assert.Equal(new[] { "-y", "@modelcontextprotocol/server-filesystem", "/data" }, fs.Args);
        Assert.Equal("https://example.com/mcp", cfg.Mcp.Servers["remote"].Url);
    }

    [Fact]
    public void Load_NoSkillsOrMcpSections_AreNull()
    {
        var cfg = Load(Base + "\nagents:\n  writer: { model: balanced }\n");

        Assert.Null(cfg.Skills);
        Assert.Null(cfg.Mcp);
    }
}
