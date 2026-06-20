namespace Agora.Skills;

/// <summary>Lookup of loaded skills by name.</summary>
public sealed class SkillRegistry
{
    private readonly Dictionary<string, Skill> _byName;

    /// <summary>Creates the registry indexing the given skills by name.</summary>
    public SkillRegistry(IEnumerable<Skill> skills)
        => _byName = skills.ToDictionary(s => s.Name);

    /// <summary>All registered skills.</summary>
    public IReadOnlyCollection<Skill> All => _byName.Values;

    /// <summary>Gets a skill by name, or null if not found.</summary>
    public Skill? Get(string name) => _byName.GetValueOrDefault(name);

    /// <summary>Resolve an agent's skill allowlist to the known skills (unknown names skipped).</summary>
    public IReadOnlyList<Skill> ForAgent(IReadOnlyList<string> names)
        => names.Select(Get).Where(s => s is not null).Select(s => s!).ToList();
}
