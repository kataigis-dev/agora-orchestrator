using Agora.Configuration;
using Agora.Providers.Concretes;

namespace Agora.Rag.Concretes;

/// <summary>
/// Fail-loud validation for the writable knowledge base. When any agent allow-lists <c>rag_write</c> the
/// conflict check must actually work; a non-semantic (<c>fake</c>) embedder or an unresolvable judge
/// model would silently degrade reconciliation to a no-op (neighbours are noise / the
/// <see cref="NoOpConflictJudge"/> always says NoConflict). This rejects such configs at <c>validate</c>
/// and again at Runtime build, so the writable KB "either works or doesn't start" and the API/CLI can't
/// bypass it. A read-only KB (no agent writes) may still use <c>fake</c>/<see cref="NoOpConflictJudge"/>.
/// </summary>
public static class RagWriteValidator
{
    /// <summary>The tool name that makes the knowledge base writable.</summary>
    public const string RagWriteTool = "rag_write";

    /// <summary>Throws <see cref="ConfigException"/> when a reachable <c>rag_write</c> lacks a semantic
    /// embedder or a resolvable conflict-judge model, naming the offending agent and field. No-op when no
    /// agent can write.</summary>
    public static void Validate(AgoraConfig config)
    {
        var writer = config.Agents.FirstOrDefault(a => a.Value.Tools.Contains(RagWriteTool));
        if (writer.Key is null)
            return; // read-only KB: fake embedder / NoOp judge are legal

        var embedderType = config.Rag?.Retrieval?.Embedder?.Type;
        if (string.IsNullOrEmpty(embedderType) || embedderType == "fake")
            throw new ConfigException(
                $"agent '{writer.Key}' uses rag_write but rag.retrieval.embedder.type is "
                + $"'{embedderType ?? "missing"}' — the conflict check needs a semantic embedder (openai/ollama)");

        var judgeAlias = config.Defaults.Model;
        if (string.IsNullOrEmpty(judgeAlias) || !ModelResolver.Resolve(config).ContainsKey(judgeAlias))
            throw new ConfigException(
                $"agent '{writer.Key}' uses rag_write but the conflict-judge model defaults.model "
                + $"('{judgeAlias ?? "missing"}') does not resolve to a configured model");
    }
}
