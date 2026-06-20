using Agora.Cli;
using Agora.Configuration;
using Xunit;

namespace Agora.Tests.Cli;

public class ConfigWizardTests
{
    private static string TempPath()
        => Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");

    private static int RunWizard(string[] lines, out string path)
    {
        var target = TempPath();
        path = target;
        var script = string.Join('\n', lines.Select(l => l == "<PATH>" ? target : l)) + "\n";
        return ConfigWizard.Run(new StringReader(script), new StringWriter(), new StringWriter());
    }

    [Fact]
    public void Init_SingleAgent_WritesValidConfig()
    {
        var code = RunWizard(new[]
        {
            "h2c",
            "n",                       // handoff?
            "openai", "OPENAI_API_KEY", "",
            "",
            "balanced", "openai", "gpt-4o",
            "",
            "balanced",
            "n",                       // skills?
            "n",                       // mcp?
            "n",                       // rag?
            "writer", "balanced", "You write the answer.",
            "",                        // finish agents
            "<PATH>",
        }, out var path);

        Assert.Equal(0, code);
        var config = ConfigLoader.Load(path);
        Assert.Equal("h2c", config.Communication);
        Assert.Null(config.Handoff);
        Assert.Equal("OPENAI_API_KEY", config.Providers["openai"].ApiKeyEnv);
        Assert.Equal("gpt-4o", config.Models["balanced"].Model);
        Assert.True(config.Agents.ContainsKey("writer"));
        Assert.Null(config.Graph);
        Assert.Null(config.Rag);
        File.Delete(path);
    }

    [Fact]
    public void Init_MultiAgent_BuildsGraphWithConditionalEdge()
    {
        var code = RunWizard(new[]
        {
            "natural",
            "n",                       // handoff?
            "openai", "OPENAI_API_KEY", "",
            "",
            "balanced", "openai", "gpt-4o",
            "",
            "balanced",
            "n",                       // skills?
            "n",                       // mcp?
            "n",                       // rag?
            "planner", "balanced", "Plan the work.",
            "critic", "balanced", "Review and signal done or fix.",
            "",                        // finish agents
            "y",                       // configure graph
            "planner",                 // entry
            "planner", "critic", "handoff",
            "critic", "planner", "conditional", "fix", "3",
            "critic", "END", "conditional", "done", "",
            "",                        // finish edges
            "<PATH>",
        }, out var path);

        Assert.Equal(0, code);
        var config = ConfigLoader.Load(path);
        Assert.NotNull(config.Graph);
        Assert.Equal("planner", config.Graph!.Entry);
        Assert.Equal(3, config.Graph.Edges.Count);
        var conditional = config.Graph.Edges.Single(e => e is { Type: "conditional", When: "fix" });
        Assert.Equal(3, conditional.MaxLoops);
        File.Delete(path);
    }

    [Fact]
    public void Init_HandoffMode_SetsFlagAndAllowsTools()
    {
        var code = RunWizard(new[]
        {
            "h2c",
            "y",                       // handoff? → enables per-agent tools prompt
            "openai", "OPENAI_API_KEY", "",
            "",
            "balanced", "openai", "gpt-4o",
            "",
            "balanced",
            "n",                       // skills?
            "n",                       // mcp?
            "n",                       // rag?
            "planner", "balanced", "Plan the work.",
            "ask_agent",               // tools (asked because handoff is on)
            "",                        // approvals
            "",                        // finish agents
            "<PATH>",
        }, out var path);

        Assert.Equal(0, code);
        var config = ConfigLoader.Load(path);
        Assert.True(config.Handoff);
        Assert.Equal(new[] { "ask_agent" }, config.Agents["planner"].Tools);
        File.Delete(path);
    }

    [Fact]
    public void Init_FullConfig_WritesSkillsMcpRagAndApprovals()
    {
        var code = RunWizard(new[]
        {
            "h2c",
            "n",                                                  // handoff?
            "openai", "OPENAI_API_KEY", "",
            "",
            "balanced", "openai", "gpt-4o",
            "",
            "balanced",
            "y", "./skills", "",                                  // skills
            "y",                                                  // mcp
            "filesystem", "stdio", "npx",
            "-y @modelcontextprotocol/server-filesystem .",       // args
            "",                                                   // finish servers
            "y",                                                  // rag
            "file", "./kb.json",                                  // vector store + path
            "./docs", "500", "100", "4", "none",                  // ingest/topk/refine
            "researcher", "balanced", "Answer questions.",
            "summarize",                                          // agent skills
            "read_file write_file rag_write",                     // agent tools
            "write_file",                                         // approvals (subset)
            "",                                                   // finish agents
            "<PATH>",
        }, out var path);

        Assert.Equal(0, code);
        var config = ConfigLoader.Load(path);

        Assert.Equal(new[] { "./skills" }, config.Skills!.Directories);
        var server = config.Mcp!.Servers["filesystem"];
        Assert.Equal("npx", server.Command);
        Assert.Contains("@modelcontextprotocol/server-filesystem", server.Args);

        var agent = config.Agents["researcher"];
        Assert.Equal(new[] { "summarize" }, agent.Skills);
        Assert.Equal(new[] { "read_file", "write_file", "rag_write" }, agent.Tools);
        Assert.Equal(new[] { "write_file" }, agent.Approvals);

        Assert.True(config.Rag!.Enabled);
        Assert.Equal("file", config.Rag.Retrieval!.VectorStore!.Type);
        Assert.Equal("./kb.json", config.Rag.Retrieval.VectorStore.Path);
        Assert.Equal(new[] { "./docs" }, config.Rag.Ingest!.Sources);
        Assert.Equal(500, config.Rag.Ingest.ChunkSize);
        Assert.Equal(4, config.Rag.Retrieval.TopK);
        File.Delete(path);
    }

    [Fact]
    public void Init_ViaCliRunner_DispatchesWizard()
    {
        var path = TempPath();
        var script = string.Join('\n', new[]
        {
            "h2c",
            "n",                       // handoff?
            "openai", "OPENAI_API_KEY", "",
            "",
            "balanced", "openai", "gpt-4o",
            "",
            "balanced",
            "n",                       // skills?
            "n",                       // mcp?
            "n",                       // rag?
            "writer", "balanced", "You write.",
            "",                        // finish agents
            "",                        // accept the --output default path
        }) + "\n";

        var code = CliRunner.Run(
            new[] { "init", "--output", path },
            @out: new StringWriter(), error: new StringWriter(), @in: new StringReader(script));

        Assert.Equal(0, code);
        Assert.True(File.Exists(path));
        File.Delete(path);
    }
}
