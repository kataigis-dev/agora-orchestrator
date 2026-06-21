using Agora.AgentFramework.Tools;
using Agora.AgentFramework.Mcp;
using Agora.AgentFramework.Specs;
using Agora.AgentFramework.Rag;
using Agora.AgentFramework.Providers;
using Agora.AgentFramework.Agents;
using Agora.Skills;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework.Tools;

/// <summary>
/// Exposes an agent's Skills as a single <c>load_skill</c> function tool (progressive disclosure):
/// the model sees the skill catalog in the description and pulls a skill's full body on demand.
/// </summary>
internal static class SkillTools
{
    /// <summary>Builds the <c>load_skill</c> function whose description lists the available skills and
    /// which returns a named skill's full body on call.</summary>
    public static AIFunction LoadSkill(IReadOnlyList<Skill> skills)
    {
        var byName = skills.ToDictionary(s => s.Name, StringComparer.Ordinal);

        string Load(string name) =>
            byName.TryGetValue(name, out var skill)
                ? skill.Body
                : $"unknown skill '{name}'. Available: {string.Join(", ", byName.Keys)}";

        var catalog = string.Join("; ", skills.Select(s =>
            $"{s.Name}{(string.IsNullOrEmpty(s.WhenToUse) ? "" : $" — {s.WhenToUse}")}"));

        return AIFunctionFactory.Create(
            Load,
            name: "load_skill",
            description:
                "Load the full step-by-step instructions for a skill by its 'name'. " +
                "Call this before performing a task a skill covers. Available skills: " + catalog);
    }
}
