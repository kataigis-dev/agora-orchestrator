using Microsoft.Extensions.AI;

namespace Agora.AgentFramework;

/// <summary>
/// Built-in <c>ask_agent</c> tool: lets an agent ask another agent a question and get its
/// answer synchronously. Created only when the agent allow-lists <c>ask_agent</c> and an
/// ask callback is available (it is absent in answer-mode sub-calls, preventing recursion).
/// </summary>
internal static class AskAgentTool
{
    public static AITool? Create(IReadOnlyList<string> allowedTools, Func<string, string, Task<string>>? ask)
    {
        var set = allowedTools.ToHashSet(StringComparer.Ordinal);
        if (!set.Contains("ask_agent") || ask is null)
            return null;

        return AIFunctionFactory.Create(
            (string target, string question) => ask(target, question),
            name: "ask_agent",
            description: "Ask another agent (by id) a question and get its answer. Use this only AFTER "
                + "rag_search fails to give you the context you need; prefer asking the agent that handed off to you.");
    }
}
