using Agora.HumanInTheLoop;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using Xunit;

namespace Agora.Tests.Rag;

public class KnowledgeBaseMutationLogTests
{
    private sealed class RecordingLog : IKbMutationLog
    {
        public List<KbMutation> Records { get; } = new();
        public Task AppendAsync(KbMutation m, CancellationToken ct = default) { Records.Add(m); return Task.CompletedTask; }
        public Task<IReadOnlyList<KbMutation>> ReadAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<KbMutation>>(Records);
        public Task<int> PurgeAsync(Func<KbMutation, bool> remove, CancellationToken ct = default)
            => Task.FromResult(Records.RemoveAll(m => remove(m)));
    }

    private sealed class AlwaysUnresolved : IConflictJudge
    {
        public Task<ConflictAssessment> AssessAsync(
            string newEntry, IReadOnlyList<Chunk> existing, CancellationToken ct = default)
            => Task.FromResult(new ConflictAssessment(
                ConflictVerdict.Unresolved, Explanation: "they contradict", Conflicting: existing));
    }

    [Fact]
    public async Task CleanAdd_LogsAddRecord()
    {
        var log = new RecordingLog();
        var kb = new KnowledgeBase(new FakeEmbedder(), new InMemoryVectorStore(), new NoOpConflictJudge(),
            mutationLog: log);

        await kb.WriteAsync("the sky is blue", agentId: "writer");

        var rec = Assert.Single(log.Records);
        Assert.Equal(KbMutationKind.Add, rec.Kind);
        Assert.Equal("writer", rec.AgentId);
        Assert.Equal("the sky is blue", rec.NewText);
        Assert.Empty(rec.OldText);
    }

    [Fact]
    public async Task HumanKeepNew_LogsReplaceRecord_WithOldTextAndDecision()
    {
        var log = new RecordingLog();
        var kb = new KnowledgeBase(new FakeEmbedder(), new InMemoryVectorStore(), new AlwaysUnresolved(),
            new FakeConflictResolver(ConflictResolution.KeepNew),
            scoreThreshold: 0.0, conflictThreshold: 0.0, mutationLog: log);

        await kb.WriteAsync("auth uses JWT", agentId: "a");        // first add (no neighbours)
        await kb.WriteAsync("auth uses OAuth", agentId: "writer"); // conflict → human keep-new → replace

        Assert.Equal(2, log.Records.Count);
        var replace = log.Records[1];
        Assert.Equal(KbMutationKind.Replace, replace.Kind);
        Assert.Equal("writer", replace.AgentId);
        Assert.Equal("auth uses OAuth", replace.NewText);
        Assert.Contains("auth uses JWT", replace.OldText);
        Assert.Equal("kept new", replace.Decision);
        Assert.Equal("they contradict", replace.JudgeExplanation);
    }

    [Fact]
    public async Task RejectedKeepExisting_LogsNoMutation()
    {
        var log = new RecordingLog();
        var kb = new KnowledgeBase(new FakeEmbedder(), new InMemoryVectorStore(), new AlwaysUnresolved(),
            new FakeConflictResolver(ConflictResolution.KeepExisting),
            scoreThreshold: 0.0, conflictThreshold: 0.0, mutationLog: log);

        await kb.WriteAsync("seed", agentId: "a");        // Add
        await kb.WriteAsync("conflicting", agentId: "a"); // Rejected → nothing changed

        Assert.Single(log.Records); // only the seed add; the rejected write recorded no mutation
    }
}
