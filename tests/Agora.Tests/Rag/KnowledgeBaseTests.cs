using Agora.HumanInTheLoop;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
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
    public async Task Resolved_RoutesToHuman_WithSuggestedMergePrefilled()
    {
        // The judge proposes a merge but never applies it: a Resolved verdict goes to the human, who
        // here accepts the suggestion (Merge with no text → the judge's reconciliation is used).
        var resolver = new FakeConflictResolver(ConflictResolution.Merge, mergedText: null);
        var kb = Build(new StubJudge(new ConflictAssessment(ConflictVerdict.Resolved, ResolvedText: "reconciled")),
            resolver, out var store);
        await kb.WriteAsync("seed");
        var result = await kb.WriteAsync("conflicting fact");

        Assert.Equal("reconciled", Assert.Single(resolver.Requests).SuggestedMerge);
        Assert.Equal(WriteOutcome.UserResolved, result.Outcome);
        Assert.Equal("reconciled", result.StoredText);
        Assert.Equal(1, await CountAsync(store)); // seed superseded only after the human confirmed
    }

    [Fact]
    public async Task Resolved_NoResolver_Rejected_NothingDeleted()
    {
        // No human available → a conflict is never auto-applied; the existing entry is left untouched.
        var kb = Build(new StubJudge(new ConflictAssessment(ConflictVerdict.Resolved, ResolvedText: "reconciled")),
            null, out var store);
        await kb.WriteAsync("seed");
        var result = await kb.WriteAsync("conflicting fact");
        Assert.Equal(WriteOutcome.Rejected, result.Outcome);
        Assert.Equal(1, await CountAsync(store)); // seed untouched — no autonomous delete/replace
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

    [Fact]
    public async Task ConcurrentWrites_AreSerialised_AllPersisted()
    {
        // N parallel writes share one KnowledgeBase (as graph branches do). Each distinct text gets a
        // distinct assessment-cache key, so without the lock the plain Dictionary and the store's List
        // would race (throw / lose entries). The lock makes the final state deterministic.
        var kb = Build(new StubJudge(new ConflictAssessment(ConflictVerdict.NoConflict)), null, out var store);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(i => kb.WriteAsync($"fact number {i}")));

        // Every write stored its entry (the first to land is Added, the rest NoConflict — never Rejected).
        Assert.All(results, r => Assert.NotEqual(WriteOutcome.Rejected, r.Outcome));
        Assert.Equal(20, await CountAsync(store)); // none lost to a race
    }
}
