using Agora.HumanInTheLoop;
using Agora.Rag;
using Xunit;

namespace Agora.Tests.Rag;

public class KnowledgeBaseTests
{
    private sealed class StubJudge : IConflictJudge
    {
        private readonly ConflictAssessment _assessment;
        public StubJudge(ConflictAssessment assessment) => _assessment = assessment;

        // Mirror LlmConflictJudge: treat the queried neighbors as the conflicting set.
        public Task<ConflictAssessment> AssessAsync(
            string newEntry, IReadOnlyList<Chunk> existing, CancellationToken cancellationToken = default)
            => Task.FromResult(_assessment with { Conflicting = _assessment.Conflicting ?? existing });
    }

    private static KnowledgeBase Build(IConflictJudge judge, IConflictResolver? resolver, out InMemoryVectorStore store)
    {
        store = new InMemoryVectorStore();
        // conflictThreshold 0.0 disables the similarity prefilter so the judge always runs here.
        return new KnowledgeBase(new FakeEmbedder(), store, judge, resolver,
            scoreThreshold: 0.0, conflictThreshold: 0.0);
    }

    private static async Task<int> CountAsync(InMemoryVectorStore store)
        => (await store.QueryAsync(new float[32], topK: 1000, scoreThreshold: 0.0)).Count;

    [Fact]
    public async Task FirstWrite_NoNeighbors_Added()
    {
        var kb = Build(new NoOpConflictJudge(), null, out var store);
        var result = await kb.WriteAsync("the sky is blue");
        Assert.Equal(WriteOutcome.Added, result.Outcome);
        Assert.Equal(1, await CountAsync(store));
    }

    [Fact]
    public async Task NoConflict_Stores()
    {
        var kb = Build(new StubJudge(new ConflictAssessment(ConflictVerdict.NoConflict)), null, out var store);
        await kb.WriteAsync("seed");
        var result = await kb.WriteAsync("another fact");
        Assert.Equal(WriteOutcome.NoConflict, result.Outcome);
        Assert.Equal(2, await CountAsync(store));
    }

    [Fact]
    public async Task Resolved_ReplacesConflictWithMergedText()
    {
        var kb = Build(new StubJudge(new ConflictAssessment(ConflictVerdict.Resolved, ResolvedText: "reconciled")),
            null, out var store);
        await kb.WriteAsync("seed");
        var result = await kb.WriteAsync("conflicting fact");
        Assert.Equal(WriteOutcome.AutoResolved, result.Outcome);
        Assert.Equal("reconciled", result.StoredText);
        Assert.Equal(1, await CountAsync(store)); // seed superseded by the reconciled entry
    }

    [Fact]
    public async Task Unresolved_KeepNew_ReplacesConflict()
    {
        var resolver = new FakeConflictResolver(ConflictResolution.KeepNew);
        var kb = Build(new StubJudge(new ConflictAssessment(ConflictVerdict.Unresolved)), resolver, out var store);
        await kb.WriteAsync("seed");
        var result = await kb.WriteAsync("conflicting fact", agentId: "writer");
        Assert.Equal(WriteOutcome.UserResolved, result.Outcome);
        Assert.Equal(1, await CountAsync(store)); // seed replaced by the new entry
        Assert.Equal("writer", Assert.Single(resolver.Requests).AgentId);
    }

    [Fact]
    public async Task Unresolved_KeepExisting_Rejected()
    {
        var kb = Build(new StubJudge(new ConflictAssessment(ConflictVerdict.Unresolved)),
            new FakeConflictResolver(ConflictResolution.KeepExisting), out var store);
        await kb.WriteAsync("seed");
        var result = await kb.WriteAsync("conflicting fact");
        Assert.Equal(WriteOutcome.Rejected, result.Outcome);
        Assert.Equal(1, await CountAsync(store));
    }

    [Fact]
    public async Task Unresolved_Merge_ReplacesConflictWithMergedText()
    {
        var kb = Build(new StubJudge(new ConflictAssessment(ConflictVerdict.Unresolved)),
            new FakeConflictResolver(ConflictResolution.Merge, mergedText: "human merge"), out var store);
        await kb.WriteAsync("seed");
        var result = await kb.WriteAsync("conflicting fact");
        Assert.Equal(WriteOutcome.UserResolved, result.Outcome);
        Assert.Equal("human merge", result.StoredText);
        Assert.Equal(1, await CountAsync(store)); // seed replaced by the merged entry
    }

    [Fact]
    public async Task Unresolved_NoResolver_Rejected()
    {
        var kb = Build(new StubJudge(new ConflictAssessment(ConflictVerdict.Unresolved)), null, out var store);
        await kb.WriteAsync("seed");
        var result = await kb.WriteAsync("conflicting fact");
        Assert.Equal(WriteOutcome.Rejected, result.Outcome);
        Assert.Equal(1, await CountAsync(store));
    }
}
