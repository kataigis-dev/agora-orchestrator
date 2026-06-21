using Agora.HumanInTheLoop;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using Xunit;

namespace Agora.Tests.Rag;

public class KnowledgeBaseEfficiencyTests
{
    private sealed class CountingJudge : IConflictJudge
    {
        private readonly ConflictAssessment _assessment;
        public int Calls { get; private set; }
        public CountingJudge(ConflictAssessment assessment) => _assessment = assessment;
        public Task<ConflictAssessment> AssessAsync(
            string newEntry, IReadOnlyList<Chunk> existing, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_assessment with { Conflicting = _assessment.Conflicting ?? existing });
        }
    }

    private static async Task<int> CountAsync(InMemoryVectorStore store)
        => (await store.QueryAsync(new float[32], topK: 1000, scoreThreshold: 0.0)).Count;

    [Fact]
    public async Task Prefilter_SkipsJudge_WhenNoCloseNeighbor()
    {
        var store = new InMemoryVectorStore();
        var judge = new CountingJudge(new ConflictAssessment(ConflictVerdict.Unresolved));
        // conflictThreshold above max cosine → the LLM judge never runs even with neighbors present.
        var kb = new KnowledgeBase(new FakeEmbedder(), store, judge, scoreThreshold: 0.0, conflictThreshold: 2.0);

        await kb.WriteAsync("seed entry");
        var result = await kb.WriteAsync("an unrelated entry");

        Assert.Equal(WriteOutcome.NoConflict, result.Outcome);
        Assert.Equal(0, judge.Calls);
        Assert.Equal(2, await CountAsync(store));
    }

    [Fact]
    public async Task Cache_AvoidsSecondJudgeCall_ForIdenticalWrite()
    {
        var store = new InMemoryVectorStore();
        var judge = new CountingJudge(new ConflictAssessment(ConflictVerdict.Unresolved));
        // KeepExisting → Rejected, so the store is unchanged and the 2nd identical write hits the cache.
        var kb = new KnowledgeBase(new FakeEmbedder(), store, judge,
            new FakeConflictResolver(ConflictResolution.KeepExisting), scoreThreshold: 0.0, conflictThreshold: 0.0);

        await kb.WriteAsync("seed entry");
        var first = await kb.WriteAsync("conflicting entry");
        var second = await kb.WriteAsync("conflicting entry");

        Assert.Equal(WriteOutcome.Rejected, first.Outcome);
        Assert.Equal(WriteOutcome.Rejected, second.Outcome);
        Assert.Equal(1, judge.Calls); // second write served from cache
    }
}
