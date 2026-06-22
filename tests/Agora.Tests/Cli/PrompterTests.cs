using Agora.Cli;
using Xunit;

namespace Agora.Tests.Cli;

public class PrompterTests
{
    private static Prompter For(string input, out StringWriter output)
    {
        output = new StringWriter();
        return new Prompter(new StringReader(input), output);
    }

    [Fact]
    public void Ask_BlankLine_ReturnsDefault()
        => Assert.Equal("def", For("\n", out _).Ask("name", "def"));

    [Fact]
    public void Ask_Value_IsTrimmed()
        => Assert.Equal("hello", For("  hello  \n", out _).Ask("name", "def"));

    [Fact]
    public void AskInt_RepromptsOnInvalid_ThenAcceptsNonNegative()
    {
        var p = For("abc\n-3\n7\n", out var output);
        Assert.Equal(7, p.AskInt("n", 0));
        Assert.Contains("enter a non-negative integer", output.ToString());
    }

    [Fact]
    public void AskInt_BlankLine_UsesDefault()
        => Assert.Equal(42, For("\n", out _).AskInt("n", 42));

    [Fact]
    public void Required_RepromptsUntilNonEmpty()
    {
        var p = For("\n\nvalue\n", out var output);
        Assert.Equal("value", p.Required("x"));
        Assert.Contains("a value is required", output.ToString());
    }

    [Fact]
    public void Choice_RepromptsUntilValidOption()
    {
        var p = For("maybe\nyes\n", out var output);
        Assert.Equal("yes", p.Choice("pick", new[] { "yes", "no" }, "no"));
        Assert.Contains("choose one of", output.ToString());
    }

    [Fact]
    public void Choice_BlankAllowed_ReturnsEmpty()
        => Assert.Equal("", For("\n", out _).Choice("pick", new[] { "a", "b" }, "", allowBlank: true));

    [Fact]
    public void AskList_SplitsOnSpacesAndCommas()
        => Assert.Equal(new[] { "a", "b", "c" }, For("a b, c\n", out _).AskList("items"));

    [Fact]
    public void AskSubset_RepromptsUntilAllAllowed()
    {
        var p = For("a x\na b\n", out var output);
        Assert.Equal(new[] { "a", "b" }, p.AskSubset("pick", new[] { "a", "b" }));
        Assert.Contains("not in tools: x", output.ToString());
    }

    [Theory]
    [InlineData("y\n", false, true)]
    [InlineData("\n", true, true)]
    [InlineData("\n", false, false)]
    [InlineData("n\n", true, false)]
    public void YesNo_HonoursDefaultsAndExplicitAnswers(string input, bool defaultYes, bool expected)
        => Assert.Equal(expected, For(input, out _).YesNo("ok?", defaultYes));

    [Fact]
    public void Ask_PastEndOfInput_Throws()
        => Assert.Throws<PrompterAbortException>(() => For("", out _).Ask("name", "def"));
}
