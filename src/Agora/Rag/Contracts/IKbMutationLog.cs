using Agora.Rag.Models;

namespace Agora.Rag.Contracts;

/// <summary>Append-only audit log of knowledge-base mutations. Records every add/replace/delete so the
/// KB has accountability and (limited) reversibility for curated knowledge. Because it retains deleted
/// content it also exposes a <see cref="PurgeAsync"/> path for retention / GDPR erasure — it is never
/// eternal-and-untouchable.</summary>
public interface IKbMutationLog
{
    /// <summary>Appends one mutation record (append-only; never rewrites existing history).</summary>
    Task AppendAsync(KbMutation mutation, CancellationToken cancellationToken = default);

    /// <summary>Reads all recorded mutations in append order.</summary>
    Task<IReadOnlyList<KbMutation>> ReadAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes every record matching <paramref name="remove"/> (e.g. older than a cutoff, or
    /// containing erased content) and returns how many were removed. The retention/purge path.</summary>
    Task<int> PurgeAsync(Func<KbMutation, bool> remove, CancellationToken cancellationToken = default);
}
