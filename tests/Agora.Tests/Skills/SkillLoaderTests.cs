using Agora.Skills;
using Xunit;

namespace Agora.Tests.Skills;

public class SkillLoaderTests : IDisposable
{
    private readonly string _root;

    public SkillLoaderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "agora-skills-" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private string WriteSkill(string folder, string content)
    {
        var dir = Path.Combine(_root, folder);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), content);
        return dir;
    }

    [Fact]
    public void Load_ParsesFrontmatterAndBody()
    {
        WriteSkill("summarize", """
            ---
            name: summarize
            description: Condensa un testo lungo.
            when_to_use: Quando l'input supera 500 parole.
            ---
            # Summarize

            Riassumi il testo mantenendo i punti chiave.
            """);

        var registry = SkillLoader.Load(new[] { _root });
        var skill = registry.Get("summarize");

        Assert.NotNull(skill);
        Assert.Equal("summarize", skill!.Name);
        Assert.Equal("Condensa un testo lungo.", skill.Description);
        Assert.Equal("Quando l'input supera 500 parole.", skill.WhenToUse);
        Assert.StartsWith("# Summarize", skill.Body);
        Assert.Contains("punti chiave", skill.Body);
    }

    [Fact]
    public void Load_NoFrontmatter_UsesFolderNameAndWholeBody()
    {
        WriteSkill("notes", "Just a plain body with no frontmatter.");

        var skill = SkillLoader.Load(new[] { _root }).Get("notes");

        Assert.NotNull(skill);
        Assert.Equal("notes", skill!.Name);
        Assert.Equal("Just a plain body with no frontmatter.", skill.Body);
        Assert.Equal("", skill.Description);
    }

    [Fact]
    public void Load_FindsSkillsRecursively()
    {
        WriteSkill(Path.Combine("nested", "deep"), """
            ---
            name: deep-skill
            ---
            body
            """);

        Assert.NotNull(SkillLoader.Load(new[] { _root }).Get("deep-skill"));
    }

    [Fact]
    public void Load_MissingDirectory_IsSkipped()
    {
        var registry = SkillLoader.Load(new[] { Path.Combine(_root, "does-not-exist") });
        Assert.Empty(registry.All);
    }

    [Fact]
    public void Registry_ForAgent_ResolvesKnownAndSkipsUnknown()
    {
        WriteSkill("a", "---\nname: a\n---\nA");
        WriteSkill("b", "---\nname: b\n---\nB");
        var registry = SkillLoader.Load(new[] { _root });

        var resolved = registry.ForAgent(new[] { "a", "ghost", "b" });

        Assert.Equal(new[] { "a", "b" }, resolved.Select(s => s.Name));
    }

    [Fact]
    public void Registry_All_ReturnsEverything()
    {
        WriteSkill("a", "---\nname: a\n---\nA");
        WriteSkill("b", "---\nname: b\n---\nB");

        Assert.Equal(2, SkillLoader.Load(new[] { _root }).All.Count);
    }
}
