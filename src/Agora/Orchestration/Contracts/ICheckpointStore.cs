using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
using System.Text.Json;

namespace Agora.Orchestration.Contracts;

/// <summary>Persists per-step run snapshots so a graph run can be resumed after a crash or interrupt.</summary>
public interface ICheckpointStore
{
    /// <summary>Persists the latest snapshot for a run, overwriting any previous one.</summary>
    void Save(string runId, StateSnapshot snapshot);

    /// <summary>Loads the snapshot for a run, or null if none exists.</summary>
    StateSnapshot? Load(string runId);
}

/// <summary>In-process checkpoint store for tests and single-run scenarios.</summary>
public sealed class InMemoryCheckpointStore : ICheckpointStore
{
    private readonly Dictionary<string, StateSnapshot> _snapshots = new();

    /// <summary>Number of save calls (useful for assertions in tests).</summary>
    public int Saves { get; private set; }

    /// <inheritdoc />
    public void Save(string runId, StateSnapshot snapshot)
    {
        _snapshots[runId] = snapshot;
        Saves++;
    }

    /// <inheritdoc />
    public StateSnapshot? Load(string runId) => _snapshots.GetValueOrDefault(runId);
}

/// <summary>File-backed checkpoint store: one JSON snapshot per run id under a directory.</summary>
public sealed class FileCheckpointStore : ICheckpointStore
{
    private readonly string _dir;

    /// <summary>Creates the store rooted at <paramref name="dir"/>, creating the directory if needed.</summary>
    public FileCheckpointStore(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(dir);
    }

    /// <summary>Resolves the JSON file path for a run id.</summary>
    private string FilePath(string runId) => Path.Combine(_dir, runId + ".json");

    /// <inheritdoc />
    public void Save(string runId, StateSnapshot snapshot)
        => File.WriteAllText(FilePath(runId), JsonSerializer.Serialize(snapshot));

    /// <inheritdoc />
    public StateSnapshot? Load(string runId)
        => File.Exists(FilePath(runId))
            ? JsonSerializer.Deserialize<StateSnapshot>(File.ReadAllText(FilePath(runId)))
            : null;
}
