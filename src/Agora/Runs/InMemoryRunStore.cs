using System.Collections.Concurrent;

namespace Agora.Runs;

public sealed class InMemoryRunStore : IRunStore
{
    private readonly ConcurrentDictionary<string, RunRecord> _runs = new();

    public RunRecord Create(string mode, string? agentId, string input)
    {
        var rec = new RunRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Mode = mode,
            AgentId = agentId,
            Input = input,
        };
        _runs[rec.Id] = rec;
        return rec;
    }

    public RunRecord? Get(string id) => _runs.GetValueOrDefault(id);

    public void Update(string id, Action<RunRecord> mutate)
    {
        if (!_runs.TryGetValue(id, out var rec))
            return;
        lock (rec)
        {
            mutate(rec);
            rec.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
