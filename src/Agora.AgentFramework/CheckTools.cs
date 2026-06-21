using Agora.Specs;
using Agora.Verification;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework;

/// <summary>
/// Built-in execution tools that make gate verdicts assert reality instead of LLM judgement:
/// <c>run_check</c> runs an allow-listed build/test command, and <c>spec_verify</c> runs the checks
/// bound to a requirement's acceptance criteria and — only if they pass deterministically — marks the
/// requirement <see cref="RequirementStatus.Verified"/>. Both are gated by the agent's allow-list and
/// can be approval-gated; checks can only be ones declared in config, never raw command lines.
/// </summary>
internal static class CheckTools
{
    /// <summary>Builds the allow-listed check tools. <c>run_check</c> needs a runner; <c>spec_verify</c>
    /// needs both a runner and a spec store.</summary>
    public static List<AITool> Create(
        IReadOnlyList<string> allowedTools, ICheckRunner? runner, ISpecStore? store, bool requireCriteria)
    {
        var tools = new List<AITool>();
        var set = allowedTools.ToHashSet(StringComparer.Ordinal);

        if (set.Contains("run_check") && runner is not null)
            tools.Add(AIFunctionFactory.Create(
                async (string name, string args) =>
                {
                    if (!runner.HasCheck(name))
                        return $"error: unknown check '{name}' (not declared under checks.commands)";
                    var result = await runner.RunAsync(name, ParseArgs(args));
                    return result.Output.Length == 0
                        ? result.ToSummary()
                        : $"{result.ToSummary()}\n{result.Output}";
                },
                name: "run_check",
                description: "Run a configured build/test check by name and return its real result "
                    + "(pass/fail from the exit code, plus output). 'args' is an optional comma-separated "
                    + "list of key=value pairs substituted into the check's {placeholders}."));

        if (set.Contains("spec_verify") && runner is not null && store is not null)
            tools.Add(AIFunctionFactory.Create(
                async (string requirementId) =>
                {
                    var doc = await store.LoadAsync();
                    if (doc.FindRequirement(requirementId) is not { } requirement)
                        return $"error: unknown requirement '{requirementId}'";

                    var result = await new AcceptanceVerifier(runner).VerifyAsync(requirement);
                    if (!result.Verified)
                        return result.ToReport();

                    // Deterministic pass → advance status and record evidence on covering tasks.
                    var verified = requirement with { Status = RequirementStatus.Verified };
                    var updated = doc.WithRequirement(verified);
                    foreach (var task in doc.Tasks.Where(t => t.RequirementIds.Contains(requirementId)))
                        updated = updated.WithTask(task with
                        {
                            Evidence = task.Evidence
                                .Append(new TaskEvidence("check", $"{requirementId} verified"))
                                .ToList(),
                        });

                    var errors = SpecValidator.Validate(updated, requireCriteria)
                        .Where(i => i.Severity == SpecSeverity.Error).ToList();
                    if (errors.Count > 0)
                        return "verified but not saved: " + string.Join("; ", errors.Select(e => e.Message));
                    await store.SaveAsync(updated);
                    return result.ToReport();
                },
                name: "spec_verify",
                description: "Verify a requirement by running the checks bound to its acceptance criteria. "
                    + "On a deterministic pass the requirement is marked Verified; otherwise the failing "
                    + "criteria are reported. Does not trust narrative claims of success."));

        return tools;
    }

    /// <summary>Parses a "k=v,k=v" argument string into a map.</summary>
    private static Dictionary<string, string> ParseArgs(string? args)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var token in (args ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = token.IndexOf('=');
            if (eq > 0) map[token[..eq].Trim()] = token[(eq + 1)..].Trim();
        }
        return map;
    }
}
