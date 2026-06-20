using System.Text.Json;

namespace Agora.Orchestration;

/// <summary>Persists per-step run snapshots so a graph run can be resumed after a crash or interrupt.</summary>
public interface ICheckpointStore
{
    void Save(string runId, StateSnapshot snapshot);
    StateSnapshot? Load(string runId);
}

/// <summary>In-process checkpoint store for tests and single-run scenarios.</summary>
public sealed class InMemoryCheckpointStore : ICheckpointStore
{
    private readonly Dictionary<string, StateSnapshot> _snapshots = new();
    public int Saves { get; private set; }

    public void Save(string runId, StateSnapshot snapshot)
    {
        _snapshots[runId] = snapshot;
        Saves++;
    }

    public StateSnapshot? Load(string runId) => _snapshots.GetValueOrDefault(runId);
}

/// <summary>File-backed checkpoint store: one JSON snapshot per run id under a directory.</summary>
public sealed class FileCheckpointStore : ICheckpointStore
{
    private readonly string _dir;

    public FileCheckpointStore(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(dir);
    }

    private string FilePath(string runId) => Path.Combine(_dir, runId + ".json");

    public void Save(string runId, StateSnapshot snapshot)
        => File.WriteAllText(FilePath(runId), JsonSerializer.Serialize(snapshot));

    public StateSnapshot? Load(string runId)
        => File.Exists(FilePath(runId))
            ? JsonSerializer.Deserialize<StateSnapshot>(File.ReadAllText(FilePath(runId)))
            : null;
}
