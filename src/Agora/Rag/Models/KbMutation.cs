namespace Agora.Rag.Models;

/// <summary>The kind of knowledge-base mutation recorded in the audit log.</summary>
public enum KbMutationKind
{
    /// <summary>A new entry was stored with nothing removed.</summary>
    Add,

    /// <summary>One or more entries were superseded (deleted) and a reconciled entry stored.</summary>
    Replace,

    /// <summary>Entries were removed without replacement.</summary>
    Delete,
}

/// <summary>One append-only audit record of a knowledge-base mutation: what was removed and added, by
/// which agent, the human's decision, the judge's explanation, and when. The vector store still hard-
/// deletes; this record is the separate, durable audit surface (it retains deleted content, so it is
/// subject to the log's retention/purge path).</summary>
public sealed record KbMutation
{
    /// <summary>What kind of mutation this was.</summary>
    public required KbMutationKind Kind { get; init; }

    /// <summary>The agent that performed the write.</summary>
    public required string AgentId { get; init; }

    /// <summary>The text(s) removed by the mutation (the superseded entries); empty for a plain add.</summary>
    public IReadOnlyList<string> OldText { get; init; } = Array.Empty<string>();

    /// <summary>The text stored by the mutation; empty for a pure delete.</summary>
    public string NewText { get; init; } = "";

    /// <summary>The human's decision (e.g. "added", "kept new", "merged"); the judge never decides alone.</summary>
    public string Decision { get; init; } = "";

    /// <summary>The conflict judge's explanation, when a conflict was assessed.</summary>
    public string JudgeExplanation { get; init; } = "";

    /// <summary>When the mutation happened (UTC).</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
