namespace Agora.Skills;

public sealed class SkillRegistry
{
    private readonly Dictionary<string, Skill> _byName;

    public SkillRegistry(IEnumerable<Skill> skills)
        => _byName = skills.ToDictionary(s => s.Name);

    public IReadOnlyCollection<Skill> All => _byName.Values;

    public Skill? Get(string name) => _byName.GetValueOrDefault(name);

    /// <summary>Resolve an agent's skill allowlist to the known skills (unknown names skipped).</summary>
    public IReadOnlyList<Skill> ForAgent(IReadOnlyList<string> names)
        => names.Select(Get).Where(s => s is not null).Select(s => s!).ToList();
}
