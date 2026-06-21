namespace Agora.Specs;

/// <summary>
/// The structured specification: requirements plus the implementation tasks that trace back to
/// them. This is the deterministic source of truth — produced by the PM/architect agents through
/// the structured spec tools, persisted via an <see cref="ISpecStore"/>, validated by
/// <see cref="SpecValidator"/>, and (in later phases) consulted by gates instead of free text.
/// Immutable: every mutation returns a new document.
/// </summary>
public sealed record SpecDocument
{
    /// <summary>Document schema version.</summary>
    public string Version { get; init; } = "1";

    /// <summary>The requirements, in stable id order.</summary>
    public IReadOnlyList<Requirement> Requirements { get; init; } = Array.Empty<Requirement>();

    /// <summary>The implementation tasks tracing back to requirements.</summary>
    public IReadOnlyList<TaskItem> Tasks { get; init; } = Array.Empty<TaskItem>();

    /// <summary>An empty specification.</summary>
    public static SpecDocument Empty { get; } = new();

    /// <summary>The requirement with the given id, or null.</summary>
    public Requirement? FindRequirement(string id) => Requirements.FirstOrDefault(r => r.Id == id);

    /// <summary>The task with the given id, or null.</summary>
    public TaskItem? FindTask(string id) => Tasks.FirstOrDefault(t => t.Id == id);

    /// <summary>Adds or replaces a requirement (matched by id), preserving order.</summary>
    public SpecDocument WithRequirement(Requirement requirement)
        => this with { Requirements = Upsert(Requirements, requirement, r => r.Id == requirement.Id) };

    /// <summary>Adds or replaces a task (matched by id), preserving order.</summary>
    public SpecDocument WithTask(TaskItem task)
        => this with { Tasks = Upsert(Tasks, task, t => t.Id == task.Id) };

    /// <summary>The next free requirement id (R{max+1}), stable across deletions.</summary>
    public string NextRequirementId() => NextId(Requirements.Select(r => r.Id), 'R');

    /// <summary>The next free task id (T{max+1}), stable across deletions.</summary>
    public string NextTaskId() => NextId(Tasks.Select(t => t.Id), 'T');

    private static IReadOnlyList<T> Upsert<T>(IReadOnlyList<T> items, T value, Func<T, bool> match)
    {
        var list = items.ToList();
        var i = list.FindIndex(new Predicate<T>(match));
        if (i >= 0) list[i] = value;
        else list.Add(value);
        return list;
    }

    private static string NextId(IEnumerable<string> ids, char prefix)
    {
        var max = ids
            .Select(id => int.TryParse(id.TrimStart(prefix), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"{prefix}{max + 1}";
    }
}
