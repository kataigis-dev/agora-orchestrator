namespace Agora.Runs;

public interface IRunStore
{
    RunRecord Create(string mode, string? agentId, string input);
    RunRecord? Get(string id);
    void Update(string id, Action<RunRecord> mutate);
}
