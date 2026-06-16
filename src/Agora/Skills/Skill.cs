namespace Agora.Skills;

/// <summary>An Agent Skill loaded from a SKILL.md folder.</summary>
public sealed record Skill
{
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public string WhenToUse { get; init; } = "";
    public required string Body { get; init; }
    public required string Directory { get; init; }
}
