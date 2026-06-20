using Agora.Rag;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework;

/// <summary>
/// Built-in tools that let an agent read from and write to the shared knowledge base:
/// <c>rag_search</c> (retrieve context) and <c>rag_write</c> (record a fact/change, with
/// conflict detection + resolution inside <see cref="KnowledgeBase"/>). Gated by the agent's
/// allow-listed tools, like <see cref="BuiltInFileTools"/>.
/// </summary>
internal static class RagTools
{
    /// <summary>Builds the allow-listed KB tools (<c>rag_search</c>/<c>rag_write</c>) available to the
    /// agent, given the RAG pipeline and knowledge base.</summary>
    public static List<AITool> Create(
        IReadOnlyList<string> allowedTools, RagPipeline? rag, KnowledgeBase? knowledgeBase, string agentId)
    {
        var set = allowedTools.ToHashSet(StringComparer.Ordinal);
        var tools = new List<AITool>();

        if (set.Contains("rag_search") && rag is not null)
            tools.Add(AIFunctionFactory.Create(
                async (string query) =>
                {
                    var enriched = await rag.RunAsync(query);
                    var context = enriched.AsContext();
                    return context.Length == 0 ? "no relevant context found" : context;
                },
                name: "rag_search",
                description: "Search the shared knowledge base for context relevant to a query. "
                    + "Use this to find missing context before asking another agent."));

        if (set.Contains("rag_write") && knowledgeBase is not null)
            tools.Add(AIFunctionFactory.Create(
                async (string text) =>
                {
                    var result = await knowledgeBase.WriteAsync(text, source: $"agent:{agentId}", agentId: agentId);
                    return $"{result.Outcome}: {result.StoredText}";
                },
                name: "rag_write",
                description: "Record a new fact or change into the shared knowledge base. Conflicts with "
                    + "existing knowledge are detected and resolved automatically or escalated to a human."));

        return tools;
    }
}
