namespace Agora.Skills;

/// <summary>An Agent Skill loaded from a SKILL.md folder.</summary>
public sealed record Skill
{
    /// <summary>Skill name (from frontmatter or the folder name).</summary>
    public required string Name { get; init; }

    /// <summary>Short description of what the skill does.</summary>
    public string Description { get; init; } = "";

    /// <summary>Guidance on when the skill should be used.</summary>
    public string WhenToUse { get; init; } = "";

    /// <summary>The Markdown body (the prompt/instructions).</summary>
    public required string Body { get; init; }

    /// <summary>Directory the skill was loaded from.</summary>
    public required string Directory { get; init; }
}
