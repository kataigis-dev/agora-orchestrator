using System.Text;
using Agora.Specs;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework;

/// <summary>
/// Built-in structured tools over the <see cref="ISpecStore"/>: <c>spec_get</c>, <c>spec_gate</c>,
/// <c>spec_propose_requirement</c>, <c>spec_bind_check</c>, <c>spec_set_status</c>, <c>spec_add_task</c>,
/// and <c>spec_link_task</c>. They replace free-text spec writes with typed operations that are validated
/// by <see cref="SpecValidator"/> before persisting (and a read-only completion gate via
/// <see cref="TraceabilityValidator"/>), so the specification stays a machine-checkable artifact. Gated
/// by the agent's allow-listed tools like <see cref="RagTools"/>.
/// </summary>
internal static class SpecTools
{
    /// <summary>Builds the allow-listed spec tools available to the agent. Returns empty when the
    /// store is absent or no spec tool is allow-listed.</summary>
    public static List<AITool> Create(IReadOnlyList<string> allowedTools, ISpecStore? store, bool requireCriteria)
    {
        var tools = new List<AITool>();
        if (store is null)
            return tools;
        var set = allowedTools.ToHashSet(StringComparer.Ordinal);

        if (set.Contains("spec_get"))
            tools.Add(AIFunctionFactory.Create(
                async () => Render(await store.LoadAsync()),
                name: "spec_get",
                description: "Read the current structured specification (requirements, their acceptance "
                    + "criteria and status, and the implementation tasks)."));

        if (set.Contains("spec_gate"))
            tools.Add(AIFunctionFactory.Create(
                async () => TraceabilityValidator.Analyze(await store.LoadAsync()).ToReport(),
                name: "spec_gate",
                description: "Check whether the specification is complete: every approved requirement "
                    + "must be covered by a task and verified through real checks. Returns the traceability "
                    + "matrix and a COMPLETE/INCOMPLETE verdict. Read-only — does not change anything. Use "
                    + "this to decide whether work is actually done before signalling completion."));

        if (set.Contains("spec_propose_requirement"))
            tools.Add(AIFunctionFactory.Create(
                async (string title, string description, string priority, string acceptance) =>
                {
                    var doc = await store.LoadAsync();
                    var id = doc.NextRequirementId();
                    var criteria = SplitLines(acceptance)
                        .Select((s, i) => new AcceptanceCriterion($"{id}.A{i + 1}", s, new SpecCheck()))
                        .ToList();
                    var requirement = new Requirement
                    {
                        Id = id,
                        Title = title,
                        Description = description ?? "",
                        Priority = ParseEnum(priority, RequirementPriority.Must),
                        Status = RequirementStatus.Proposed,
                        AcceptanceCriteria = criteria,
                    };
                    return await Commit(store, doc.WithRequirement(requirement), requireCriteria, $"added requirement {id}");
                },
                name: "spec_propose_requirement",
                description: "Propose a new requirement. 'priority' is must/should/could/wont. "
                    + "'acceptance' is one acceptance criterion per line (each becomes a checkable criterion). "
                    + "Returns the assigned requirement id (R1..Rn)."));

        if (set.Contains("spec_bind_check"))
            tools.Add(AIFunctionFactory.Create(
                async (string requirementId, string criterionId, string kind, string expression) =>
                {
                    var doc = await store.LoadAsync();
                    if (doc.FindRequirement(requirementId) is not { } requirement)
                        return $"error: unknown requirement '{requirementId}'";
                    var criteria = requirement.AcceptanceCriteria.ToList();
                    var index = criteria.FindIndex(c => c.Id == criterionId);
                    if (index < 0)
                        return $"error: unknown criterion '{criterionId}' on '{requirementId}'";
                    criteria[index] = criteria[index] with
                    {
                        Check = new SpecCheck(ParseEnum(kind, CheckKind.Manual), expression ?? ""),
                    };
                    var updated = requirement with { AcceptanceCriteria = criteria };
                    return await Commit(store, doc.WithRequirement(updated), requireCriteria,
                        $"bound {criterionId} -> {kind} {expression}");
                },
                name: "spec_bind_check",
                description: "Bind an acceptance criterion to a check so it can be machine-verified. "
                    + "'kind' is test/command/fileexists/manual; 'expression' is the configured check name "
                    + "(with optional key=value args) for test/command, or a path for fileexists."));

        if (set.Contains("spec_set_status"))
            tools.Add(AIFunctionFactory.Create(
                async (string requirementId, string status) =>
                {
                    var doc = await store.LoadAsync();
                    if (doc.FindRequirement(requirementId) is not { } requirement)
                        return $"error: unknown requirement '{requirementId}'";
                    var updated = requirement with { Status = ParseEnum(status, requirement.Status) };
                    return await Commit(store, doc.WithRequirement(updated), requireCriteria,
                        $"{requirementId} -> {updated.Status}");
                },
                name: "spec_set_status",
                description: "Set a requirement's lifecycle status: proposed/approved/implemented/verified/rejected."));

        if (set.Contains("spec_add_task"))
            tools.Add(AIFunctionFactory.Create(
                async (string description, string kind, string requirementIds) =>
                {
                    var doc = await store.LoadAsync();
                    var id = doc.NextTaskId();
                    var task = new TaskItem
                    {
                        Id = id,
                        Description = description,
                        Kind = kind ?? "",
                        RequirementIds = SplitIds(requirementIds),
                    };
                    return await Commit(store, doc.WithTask(task), requireCriteria, $"added task {id}");
                },
                name: "spec_add_task",
                description: "Add an implementation task. 'requirementIds' is a comma/space-separated list of "
                    + "requirement ids it implements (must exist). Returns the assigned task id (T1..Tn)."));

        if (set.Contains("spec_link_task"))
            tools.Add(AIFunctionFactory.Create(
                async (string taskId, string requirementIds) =>
                {
                    var doc = await store.LoadAsync();
                    if (doc.FindTask(taskId) is not { } task)
                        return $"error: unknown task '{taskId}'";
                    var merged = task.RequirementIds.Concat(SplitIds(requirementIds)).Distinct().ToList();
                    return await Commit(store, doc.WithTask(task with { RequirementIds = merged }), requireCriteria,
                        $"{taskId} now covers {string.Join(", ", merged)}");
                },
                name: "spec_link_task",
                description: "Link an existing task to additional requirement ids (comma/space-separated)."));

        return tools;
    }

    /// <summary>Validates the candidate document; persists it and returns <paramref name="success"/>
    /// on success, or a "rejected" message listing the blocking errors (nothing is saved).</summary>
    private static async Task<string> Commit(ISpecStore store, SpecDocument candidate, bool requireCriteria, string success)
    {
        var errors = SpecValidator.Validate(candidate, requireCriteria)
            .Where(i => i.Severity == SpecSeverity.Error)
            .ToList();
        if (errors.Count > 0)
            return "rejected (nothing saved): " + string.Join("; ", errors.Select(e => e.Message));
        await store.SaveAsync(candidate);
        return "ok: " + success;
    }

    private static List<string> SplitLines(string? text) => (text ?? "")
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();

    private static List<string> SplitIds(string? text) => (text ?? "")
        .Split(new[] { ',', ' ', ';', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();

    private static T ParseEnum<T>(string? value, T fallback) where T : struct, Enum
        => Enum.TryParse<T>(value, ignoreCase: true, out var parsed) ? parsed : fallback;

    /// <summary>Renders the spec compactly for <c>spec_get</c>.</summary>
    private static string Render(SpecDocument doc)
    {
        if (doc.Requirements.Count == 0 && doc.Tasks.Count == 0)
            return "(empty specification)";
        var sb = new StringBuilder();
        foreach (var r in doc.Requirements)
        {
            sb.AppendLine($"{r.Id} [{r.Priority}/{r.Status}] {r.Title}");
            foreach (var c in r.AcceptanceCriteria)
                sb.AppendLine($"  - {c.Id} ({c.Check.Kind}) {c.Statement}");
        }
        foreach (var t in doc.Tasks)
            sb.AppendLine($"{t.Id} [{t.Status}] {t.Description} -> {string.Join(", ", t.RequirementIds)}");
        return sb.ToString().TrimEnd();
    }
}
