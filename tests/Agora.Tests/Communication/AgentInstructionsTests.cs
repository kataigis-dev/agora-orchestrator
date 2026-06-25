using Agora.Agents.Contracts;
using Agora.Communication;
using Agora.Configuration;
using Xunit;

namespace Agora.Tests.Communication;

public class AgentInstructionsTests
{
    private static AgentInstructions Build(string communication, bool handoff, string? language)
    {
        var yaml =
            $"communication: {communication}\n" +
            $"handoff: {handoff.ToString().ToLowerInvariant()}\n" +
            (language is null ? "" : $"language: {language}\n") +
            "providers: { openai: { api_key_env: K } }\n" +
            "models: { m: { provider: openai, model: x } }\n" +
            "agents: { a: { model: m, role: \"r\" } }\n";
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, yaml);
        try { return AgentInstructions.For(ConfigLoader.Load(path)); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Natural_NoDirectives_EmptyPrefix_AndSignalInterpreter()
    {
        var instructions = Build("natural", handoff: false, language: null);
        Assert.Equal("", instructions.Prefix(answerMode: false));
        Assert.IsType<SignalInterpreter>(instructions.Interpreter);
    }

    [Fact]
    public void H2c_PrefixCarriesProtocol_AndH2cInterpreter()
    {
        var instructions = Build("h2c", handoff: false, language: null);
        Assert.Contains("COMMUNICATION PROTOCOL: H2C", instructions.Prefix(answerMode: false));
        Assert.IsType<H2cInterpreter>(instructions.Interpreter);
    }

    [Fact]
    public void Handoff_PresentInNormalMode_SuppressedInAnswerMode()
    {
        var instructions = Build("natural", handoff: true, language: null);
        Assert.Contains("HANDOFF MODE", instructions.Prefix(answerMode: false));
        Assert.DoesNotContain("HANDOFF MODE", instructions.Prefix(answerMode: true));
    }

    [Fact]
    public void Language_DirectiveCarriesLanguageName()
    {
        var instructions = Build("natural", handoff: false, language: "Italian");
        Assert.Contains("OUTPUT LANGUAGE", instructions.Prefix(answerMode: false));
        Assert.Contains("Italian", instructions.Prefix(answerMode: false));
    }

    [Fact]
    public void Prefix_OrdersLanguageThenProtocolThenHandoff()
    {
        var prefix = Build("h2c", handoff: true, language: "Italian").Prefix(answerMode: false);
        var language = prefix.IndexOf("OUTPUT LANGUAGE", StringComparison.Ordinal);
        var protocol = prefix.IndexOf("COMMUNICATION PROTOCOL: H2C", StringComparison.Ordinal);
        var handoff = prefix.IndexOf("HANDOFF MODE", StringComparison.Ordinal);
        Assert.True(language >= 0 && protocol >= 0 && handoff >= 0);
        Assert.True(language < protocol, "language should precede the protocol preamble");
        Assert.True(protocol < handoff, "protocol preamble should precede the handoff directive");
    }
}
