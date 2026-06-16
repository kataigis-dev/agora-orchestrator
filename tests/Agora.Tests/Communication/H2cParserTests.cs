using Agora.Communication;
using Xunit;

namespace Agora.Tests.Communication;

public class H2cParserTests
{
    private readonly H2cParser _parser = new();

    [Fact]
    public void Parse_BlockWithFields()
    {
        var blocks = _parser.Parse("[ARCH:PLAN]\nid:api-meteo|fw:python3.11|lib:[fastapi,httpx]");
        var b = Assert.Single(blocks);
        Assert.Equal("ARCH", b.Type);
        Assert.Equal("PLAN", b.Subtype);
        Assert.Equal("api-meteo", b.Fields["id"]);
        Assert.Equal("python3.11", b.Fields["fw"]);
        Assert.Equal("[fastapi,httpx]", b.Fields["lib"]);
    }

    [Fact]
    public void Parse_HeaderOnly_NoFields()
    {
        var blocks = _parser.Parse("[STATE:DONE]");
        Assert.Equal("DONE", blocks[0].Subtype);
        Assert.Empty(blocks[0].Fields);
    }

    [Fact]
    public void Parse_IgnoresSurroundingProse_AndFindsMultipleBlocks()
    {
        var text = "Here is my plan:\n[ARCH:PLAN]\nid:x\nSome notes.\n[TEST:PASS]\nn:3";
        var blocks = _parser.Parse(text);
        Assert.Equal(2, blocks.Count);
        Assert.Equal("PLAN", blocks[0].Subtype);
        Assert.Equal("PASS", blocks[1].Subtype);
        Assert.Equal("3", blocks[1].Fields["n"]);
    }

    [Fact]
    public void Parse_NonH2cText_ReturnsEmpty() => Assert.Empty(_parser.Parse("just a plain sentence."));

    [Fact]
    public void Serialize_RoundTrips()
    {
        var text = "[ARCH:PLAN]\nid:x|fw:net10";
        var roundTripped = _parser.Serialize(_parser.Parse(text));
        Assert.Equal(text, roundTripped);
    }

    [Fact]
    public void IsValid_KnownTypeAndSubtype_True()
    {
        Assert.True(_parser.IsValid(new H2cBlock { Type = "STATE", Subtype = "DONE", Fields = new Dictionary<string, string>() }));
        Assert.False(_parser.IsValid(new H2cBlock { Type = "BOGUS", Subtype = "DONE", Fields = new Dictionary<string, string>() }));
    }
}
