using Agora.Cli;
using Agora.Configuration;
using Xunit;

namespace Agora.Tests.Cli;

/// <summary>The CLI refuses a run that could reach <c>rag_write</c> when the process is not interactive
/// (Agora is CLI-only with a human always present). Tested at the guard directly so it is independent of
/// the embedder/backend build path.</summary>
public class RagWriteInteractivityTests
{
    private static ConfigState State(bool interactive) => new(
        new Dictionary<string, string>(), Provider: null, Backend: null, ApprovalHandler: null,
        ConflictResolver: null, In: TextReader.Null, Out: TextWriter.Null, Error: TextWriter.Null,
        Interactive: interactive);

    private static AgoraConfig WithAgent(string id, params string[] tools)
    {
        var cfg = new AgoraConfig();
        var agent = new AgentConfig { Model = "m" };
        agent.Tools.AddRange(tools);
        cfg.Agents[id] = agent;
        return cfg;
    }

    [Fact]
    public void NonInteractive_WithRagWriteAgent_Refused()
    {
        var ex = Assert.Throws<ConfigException>(() =>
            State(interactive: false).RequireInteractiveForRagWrite(WithAgent("kbwriter", "rag_write")));
        Assert.Contains("rag_write", ex.Message);
        Assert.Contains("interactive", ex.Message);
        Assert.Contains("kbwriter", ex.Message);
    }

    [Fact]
    public void Interactive_WithRagWriteAgent_Allowed()
        // A human is present → the synchronous conflict gate can run; no refusal.
        => State(interactive: true).RequireInteractiveForRagWrite(WithAgent("kbwriter", "rag_write"));

    [Fact]
    public void NonInteractive_WithoutRagWrite_Allowed()
        // The guard is scoped to rag_write; a non-writing config runs fine non-interactively.
        => State(interactive: false).RequireInteractiveForRagWrite(WithAgent("writer"));
}
