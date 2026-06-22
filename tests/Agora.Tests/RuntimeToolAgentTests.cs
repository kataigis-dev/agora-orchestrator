using Agora;
using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Xunit;

namespace Agora.Tests;

public class RuntimeToolAgentTests : IDisposable
{
    private readonly string _skillsDir;

    public RuntimeToolAgentTests()
    {
        _skillsDir = Path.Combine(Path.GetTempPath(), "agora-rt-skills-" + Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(_skillsDir, "summarize"));
        File.WriteAllText(
            Path.Combine(_skillsDir, "summarize", "SKILL.md"),
            "---\nname: summarize\ndescription: Condensa.\n---\nRiassumi il testo.");
    }

    public void Dispose()
    {
        if (Directory.Exists(_skillsDir))
            Directory.Delete(_skillsDir, recursive: true);
    }

    private sealed class FakeToolAgentFactory : IAgentBackend
    {
        public AgentBuildContext? Last { get; private set; }
        public IAgent CreateToolAgent(AgentBuildContext context)
        {
            Last = context;
            return new StubAgent();
        }
    }

    private sealed class StubAgent : IAgent
    {
        public Task<AgentResult> RunAsync(string userInput, string context = "", Action<string>? onChunk = null)
            => Task.FromResult(new AgentResult { Output = "stub" });
    }

    private Runtime Build(FakeChatProvider provider, IAgentBackend? factory)
    {
        var yaml = $$"""
            providers:
              openai: { api_key_env: OPENAI_API_KEY }
            models:
              balanced: { provider: openai, model: gpt-4o }
            agents:
              plain: { model: balanced, role: "You write." }
              tooled: { model: balanced, skills: [summarize], tools: [search] }
            skills:
              directories: ["{{_skillsDir.Replace("\\", "/")}}"]
            """;
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, yaml);
        return Runtime.FromConfig(path, provider, backend: factory);
    }

    [Fact]
    public void BuildAgent_NoSkillsOrTools_ReturnsCoreAgent()
    {
        var agent = Build(new FakeChatProvider(new[] { "x" }), new FakeToolAgentFactory()).BuildAgent("plain");
        Assert.IsType<Agent>(agent);
    }

    [Fact]
    public void BuildAgent_WithSkillsOrTools_DelegatesToFactoryWithResolvedSkills()
    {
        var factory = new FakeToolAgentFactory();
        var agent = Build(new FakeChatProvider(new[] { "x" }), factory).BuildAgent("tooled");

        Assert.IsType<StubAgent>(agent);
        Assert.NotNull(factory.Last);
        Assert.Equal("tooled", factory.Last!.Card.Id);
        Assert.Equal(new[] { "summarize" }, factory.Last.Skills.Select(s => s.Name));
        Assert.Equal(new[] { "search" }, factory.Last.Card.Tools);
    }

    [Fact]
    public void BuildAgent_SkillsOrTools_NoFactory_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Build(new FakeChatProvider(new[] { "x" }), factory: null).BuildAgent("tooled"));
        Assert.Contains("no IAgentBackend", ex.Message);
    }
}
