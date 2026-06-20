namespace Agora.Configuration;

/// <summary>Skills configuration: where to discover <c>SKILL.md</c> files.</summary>
public sealed class SkillsConfig
{
    /// <summary>Directories scanned for skills.</summary>
    public List<string> Directories { get; set; } = new();
}
