using Agora.Runs.Contracts;
using Agora.Runs.Models;
using Agora.Runs.Concretes;
namespace Agora.Runs.Contracts;

/// <summary>Stores the lifecycle records of API runs.</summary>
public interface IRunStore
{
    /// <summary>Creates and stores a new run record, returning it.</summary>
    RunRecord Create(string mode, string? agentId, string input);

    /// <summary>Gets a run by id, or null if unknown.</summary>
    RunRecord? Get(string id);

    /// <summary>Atomically mutates a run record (no-op if the id is unknown).</summary>
    void Update(string id, Action<RunRecord> mutate);
}
