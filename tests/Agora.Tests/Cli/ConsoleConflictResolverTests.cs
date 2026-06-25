using Agora.Cli;
using Agora.HumanInTheLoop;
using Xunit;

namespace Agora.Tests.Cli;

public class ConsoleConflictResolverTests
{
    private static ConflictResolutionRequest Req(string suggestedMerge = "") => new()
    {
        AgentId = "writer",
        NewEntry = "auth uses OAuth",
        ExistingEntries = new[] { "auth uses JWT" },
        Explanation = "they contradict",
        SuggestedMerge = suggestedMerge,
    };

    [Theory]
    [InlineData("e", ConflictResolution.KeepExisting)]
    [InlineData("", ConflictResolution.KeepExisting)]
    [InlineData("n", ConflictResolution.KeepNew)]
    public async Task ResolveAsync_ReadsDecisionFromInput(string input, ConflictResolution expected)
    {
        var decision = await new ConsoleConflictResolver(new StringReader(input + "\n"), new StringWriter())
            .ResolveAsync(Req());
        Assert.Equal(expected, decision.Resolution);
    }

    [Fact]
    public async Task ResolveAsync_Merge_EmptyLineAcceptsJudgeSuggestion()
    {
        // "m" then an empty merged-text line → accept the judge's suggested reconciliation.
        var decision = await new ConsoleConflictResolver(new StringReader("m\n\n"), new StringWriter())
            .ResolveAsync(Req(suggestedMerge: "auth uses JWT for sessions and OAuth for delegation"));
        Assert.Equal(ConflictResolution.Merge, decision.Resolution);
        Assert.Equal("auth uses JWT for sessions and OAuth for delegation", decision.MergedText);
    }

    [Fact]
    public async Task ResolveAsync_Merge_TypedTextOverridesSuggestion()
    {
        var decision = await new ConsoleConflictResolver(new StringReader("m\nhuman wording\n"), new StringWriter())
            .ResolveAsync(Req(suggestedMerge: "judge wording"));
        Assert.Equal(ConflictResolution.Merge, decision.Resolution);
        Assert.Equal("human wording", decision.MergedText);
    }

    [Fact]
    public async Task ResolveAsync_ShowsSuggestedMerge()
    {
        var output = new StringWriter();
        await new ConsoleConflictResolver(new StringReader("e\n"), output)
            .ResolveAsync(Req(suggestedMerge: "the reconciled statement"));
        Assert.Contains("the reconciled statement", output.ToString());
    }
}
