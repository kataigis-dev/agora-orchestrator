using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using Xunit;

namespace Agora.Tests.Rag;

public class LlmConflictJudgeTests
{
    private static readonly ModelSpec Spec = new() { Alias = "j", Provider = "fake", Model = "m" };
    private static readonly IReadOnlyList<Chunk> Existing = new[] { new Chunk("old fact", "s") };

    private static Task<ConflictAssessment> Assess(string response)
        => new LlmConflictJudge(new FakeChatProvider(new[] { response }), Spec)
            .AssessAsync("new fact", Existing);

    [Fact]
    public async Task Parses_NoConflict()
        => Assert.Equal(ConflictVerdict.NoConflict,
            (await Assess("VERDICT: NO_CONFLICT\nEXPLANATION: unrelated")).Verdict);

    [Fact]
    public async Task Parses_Resolved_WithMergedText()
    {
        var a = await Assess(
            "VERDICT: RESOLVED\nCONFLICTS_WITH: 1\nRESOLUTION: merged statement here\nEXPLANATION: combined");
        Assert.Equal(ConflictVerdict.Resolved, a.Verdict);
        Assert.Equal("merged statement here", a.ResolvedText);
    }

    [Fact]
    public async Task Parses_Unresolved()
    {
        var a = await Assess("VERDICT: UNRESOLVED\nCONFLICTS_WITH: 1\nEXPLANATION: needs a human");
        Assert.Equal(ConflictVerdict.Unresolved, a.Verdict);
        Assert.Contains("human", a.Explanation);
    }

    [Fact]
    public async Task Unknown_DefaultsToNoConflict()
        => Assert.Equal(ConflictVerdict.NoConflict, (await Assess("I think these are fine.")).Verdict);

    [Fact]
    public async Task ConflictVerdict_WithNoNamedEntry_FailsNarrowToNoConflict()
    {
        // RESOLVED/UNRESOLVED but CONFLICTS_WITH: NONE names no entry → nothing to delete/escalate.
        Assert.Equal(ConflictVerdict.NoConflict,
            (await Assess("VERDICT: RESOLVED\nCONFLICTS_WITH: NONE\nRESOLUTION: merged")).Verdict);
        Assert.Equal(ConflictVerdict.NoConflict,
            (await Assess("VERDICT: UNRESOLVED\nEXPLANATION: vague, no list")).Verdict);
    }

    [Fact]
    public async Task UnparseableConflictsWith_NeverFallsBackToAll()
    {
        var existing = new[] { new Chunk("a", "s"), new Chunk("b", "s"), new Chunk("c", "s") };
        var judge = new LlmConflictJudge(
            new FakeChatProvider(new[] { "VERDICT: UNRESOLVED\nCONFLICTS_WITH: garbage\nEXPLANATION: x" }), Spec);
        var a = await judge.AssessAsync("new", existing);
        // Old behaviour returned all three as conflicting; fail-narrow names none → no conflict.
        Assert.Equal(ConflictVerdict.NoConflict, a.Verdict);
    }

    [Fact]
    public async Task Resolved_PicksConflictingSubset()
    {
        var existing = new[] { new Chunk("a", "s"), new Chunk("b", "s"), new Chunk("c", "s") };
        var judge = new LlmConflictJudge(
            new FakeChatProvider(new[] { "VERDICT: RESOLVED\nCONFLICTS_WITH: 2\nRESOLUTION: merged" }), Spec);
        var a = await judge.AssessAsync("new", existing);
        Assert.Equal(ConflictVerdict.Resolved, a.Verdict);
        Assert.Equal(new[] { "b" }, a.Conflicting!.Select(c => c.Text));
    }
}
