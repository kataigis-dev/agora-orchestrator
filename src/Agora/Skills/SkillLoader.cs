using YamlDotNet.Serialization;

namespace Agora.Skills;

/// <summary>Loads Agent Skills from SKILL.md files (YAML frontmatter + Markdown body).</summary>
public static class SkillLoader
{
    public static SkillRegistry Load(IReadOnlyList<string> directories)
    {
        var skills = new List<Skill>();
        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
                continue;
            foreach (var skillMd in Directory.EnumerateFiles(dir, "SKILL.md", SearchOption.AllDirectories).OrderBy(p => p))
                skills.Add(Parse(skillMd));
        }
        return new SkillRegistry(skills);
    }

    internal static Skill Parse(string path)
    {
        var text = File.ReadAllText(path);
        var dir = Path.GetDirectoryName(path)!;
        var frontmatter = "";
        var body = text;
        if (text.StartsWith("---", StringComparison.Ordinal))
        {
            var end = text.IndexOf("\n---", 3, StringComparison.Ordinal);
            if (end >= 0)
            {
                frontmatter = text.Substring(3, end - 3);
                body = text[(end + 4)..].TrimStart('\r', '\n');
            }
        }
        var meta = ParseFrontmatter(frontmatter);
        return new Skill
        {
            Name = meta.GetValueOrDefault("name") ?? Path.GetFileName(dir),
            Description = meta.GetValueOrDefault("description") ?? "",
            WhenToUse = meta.GetValueOrDefault("when_to_use") ?? "",
            Body = body.Trim(),
            Directory = dir,
        };
    }

    private static Dictionary<string, string> ParseFrontmatter(string frontmatter)
    {
        if (string.IsNullOrWhiteSpace(frontmatter))
            return new Dictionary<string, string>();
        var deserializer = new DeserializerBuilder().Build();
        return deserializer.Deserialize<Dictionary<string, string>>(frontmatter) ?? new Dictionary<string, string>();
    }
}
