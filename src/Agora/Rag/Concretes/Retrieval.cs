using Agora.Agents.Contracts;
using Agora.Configuration;
using Agora.HumanInTheLoop;
using Agora.Providers.Contracts;
using Agora.Providers.Concretes;
using Agora.Providers.Models;
using Agora.Rag.Contracts;

namespace Agora.Rag.Concretes;

/// <summary>
/// The retrieval subsystem as one deep module. It owns the shared embedder + vector store and, over
/// them, the read <see cref="Pipeline"/>, the agent write path <see cref="KnowledgeBase"/>, and the
/// run-time context <see cref="Memory"/>. Building it concentrates two invariants that callers used to
/// hand-wire: reads and writes share one store, and the parts are built in order (stores → pipeline →
/// knowledge base → memory). <see cref="Pipeline"/>/<see cref="KnowledgeBase"/> are null when RAG is
/// off; <see cref="Memory"/> is null when context memory is off; <see cref="Build"/> returns null only
/// when both are off.
/// </summary>
public sealed class Retrieval
{
    private readonly IEmbedder _embedder;
    private readonly IVectorStore _store;

    private Retrieval(
        IEmbedder embedder, IVectorStore store,
        RagPipeline? pipeline, KnowledgeBase? knowledgeBase, ContextMemory? memory)
    {
        _embedder = embedder;
        _store = store;
        Pipeline = pipeline;
        KnowledgeBase = knowledgeBase;
        Memory = memory;
    }

    /// <summary>The read pipeline (refine → retrieve), or null when RAG is disabled.</summary>
    public RagPipeline? Pipeline { get; }

    /// <summary>The agent-facing write path into the shared store, or null when RAG is disabled.</summary>
    public KnowledgeBase? KnowledgeBase { get; }

    /// <summary>RAG-backed context memory for graph runs, or null when memory is disabled.</summary>
    public ContextMemory? Memory { get; }

    /// <summary>Ingests the given text sources into the shared store and returns the chunk count.</summary>
    public Task<int> IngestAsync(
        IReadOnlyList<string> sources, int chunkSize, int overlap, CancellationToken cancellationToken = default)
        => new Ingestor(_embedder, _store, chunkSize, overlap).IngestPathsAsync(sources, cancellationToken);

    /// <summary>
    /// Builds the subsystem from config, resolving the refiner/judge model specs internally. Returns
    /// null only when both RAG and context memory are disabled. Memory reuses the RAG embedder/store
    /// when RAG is on (entries are source-tagged to stay distinct from knowledge-base facts) and
    /// otherwise falls back to an offline in-memory store.
    /// </summary>
    public static Retrieval? Build(
        AgoraConfig config, IChatProvider provider, IAgentBackend? backend = null,
        IConflictResolver? conflictResolver = null)
    {
        var ragOn = config.Rag is { Enabled: true };
        var memoryOn = config.Memory?.Enabled == true;
        if (!ragOn && !memoryOn)
            return null;

        var specs = ModelResolver.Resolve(config);
        ModelSpec? refineSpec = config.Rag?.Refine?.Model is string refineAlias
            && specs.TryGetValue(refineAlias, out var rs) ? rs : null;
        ModelSpec? judgeSpec = config.Defaults.Model is string defaultAlias
            && specs.TryGetValue(defaultAlias, out var js) ? js : null;

        RagPipeline? pipeline = null;
        KnowledgeBase? knowledgeBase = null;
        IEmbedder? embedder = null;
        IVectorStore? store = null;

        if (ragOn)
        {
            (embedder, store) = RagFactory.BuildStores(config, backend);
            pipeline = RagFactory.BuildPipeline(config.Rag!, provider, refineSpec, embedder, store);
            IConflictJudge judge = judgeSpec is not null
                ? new LlmConflictJudge(provider, judgeSpec)
                : new NoOpConflictJudge();
            knowledgeBase = new KnowledgeBase(embedder, store, judge, conflictResolver);
        }

        ContextMemory? memory = null;
        if (memoryOn)
        {
            embedder ??= new FakeEmbedder();
            store ??= new InMemoryVectorStore();
            memory = new ContextMemory(embedder, store);
        }

        return new Retrieval(embedder!, store!, pipeline, knowledgeBase, memory);
    }

    /// <summary>Wraps an already-built pipeline (and an optional memory) as a retrieval subsystem,
    /// adding a knowledge base over the pipeline's shared store. Used by tests that pre-populate a
    /// store and need reads and writes to hit that same instance.</summary>
    internal static Retrieval ForPipeline(RagPipeline pipeline, ContextMemory? memory = null)
        => new(
            pipeline.Embedder, pipeline.Store, pipeline,
            new KnowledgeBase(pipeline.Embedder, pipeline.Store, new NoOpConflictJudge()), memory);
}
