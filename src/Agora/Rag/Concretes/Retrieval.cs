using Agora.Agents.Contracts;
using Agora.Configuration;
using Agora.HumanInTheLoop;
using Agora.Providers.Contracts;
using Agora.Providers.Concretes;
using Agora.Providers.Models;
using Agora.Rag.Contracts;

namespace Agora.Rag.Concretes;

/// <summary>
/// The retrieval subsystem as one deep module. It owns the KB embedder + vector store and, over them,
/// the read <see cref="Pipeline"/> and the agent write path <see cref="KnowledgeBase"/>; run-time
/// context memory is minted per run by <see cref="NewMemory"/> over a separate, in-memory store of its
/// own. Building it concentrates invariants that callers used to hand-wire: KB reads and writes share
/// one store, memory is kept in its own per-run store, and the parts are built in order.
/// <see cref="Pipeline"/>/<see cref="KnowledgeBase"/> are null when RAG is off; <see cref="NewMemory"/>
/// returns null when context memory is off; <see cref="Build"/> returns null only when both are off.
/// </summary>
public sealed class Retrieval
{
    private readonly IEmbedder? _embedder;
    private readonly IVectorStore? _store;
    private readonly IEmbedder? _memoryEmbedder;

    private Retrieval(
        IEmbedder? embedder, IVectorStore? store,
        RagPipeline? pipeline, KnowledgeBase? knowledgeBase, IEmbedder? memoryEmbedder)
    {
        _embedder = embedder;
        _store = store;
        Pipeline = pipeline;
        KnowledgeBase = knowledgeBase;
        _memoryEmbedder = memoryEmbedder;
    }

    /// <summary>The read pipeline (refine → retrieve), or null when RAG is disabled.</summary>
    public RagPipeline? Pipeline { get; }

    /// <summary>The agent-facing write path into the shared store, or null when RAG is disabled.</summary>
    public KnowledgeBase? KnowledgeBase { get; }

    /// <summary>True when context memory is enabled; mint a per-run instance with <see cref="NewMemory"/>.</summary>
    public bool MemoryEnabled => _memoryEmbedder is not null;

    /// <summary>Mints a fresh, empty context memory over its own in-memory store, or null when memory is
    /// disabled. Each graph run gets its own instance, so run-time context is per-run and discarded at run
    /// end and is never written to the KB's (possibly persistent) store. The embedder is stateless, so
    /// sharing it across runs is safe.</summary>
    public ContextMemory? NewMemory()
        => _memoryEmbedder is null ? null : new ContextMemory(_memoryEmbedder, new InMemoryVectorStore());

    /// <summary>Ingests the given text sources into the KB store and returns the chunk count.
    /// Requires RAG to be enabled (there is no KB store otherwise).</summary>
    public Task<int> IngestAsync(
        IReadOnlyList<string> sources, int chunkSize, int overlap, CancellationToken cancellationToken = default)
    {
        if (_embedder is null || _store is null)
            throw new InvalidOperationException("ingest requires RAG to be enabled");
        return new Ingestor(_embedder, _store, chunkSize, overlap).IngestPathsAsync(sources, cancellationToken);
    }

    /// <summary>
    /// Builds the subsystem from config, resolving the refiner/judge model specs internally. Returns
    /// null only when both RAG and context memory are disabled. Memory is minted per run by
    /// <see cref="NewMemory"/> over its own in-memory store (separate from the KB) and reuses the RAG
    /// embedder when present, else the offline fake one.
    /// </summary>
    public static Retrieval? Build(
        AgoraConfig config, IChatProvider provider, IAgentBackend? backend = null,
        IConflictResolver? conflictResolver = null, IKbMutationLog? mutationLog = null)
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
            knowledgeBase = new KnowledgeBase(embedder, store, judge, conflictResolver, mutationLog: mutationLog);
        }

        // Memory mints its own in-memory store per run (see NewMemory), so the two never mix and
        // run-time context is never persisted into the KB. The embedder is stateless, so reusing the
        // RAG one (when present) is fine; otherwise fall back to the offline fake embedder.
        IEmbedder? memoryEmbedder = memoryOn ? embedder ?? new FakeEmbedder() : null;

        return new Retrieval(embedder, store, pipeline, knowledgeBase, memoryEmbedder);
    }

    /// <summary>Wraps an already-built pipeline as a retrieval subsystem, adding a knowledge base over
    /// the pipeline's shared store. Used by tests that pre-populate a store and need reads and writes to
    /// hit that same instance. Context memory is off on this path.</summary>
    internal static Retrieval ForPipeline(RagPipeline pipeline)
        => new(
            pipeline.Embedder, pipeline.Store, pipeline,
            new KnowledgeBase(pipeline.Embedder, pipeline.Store, new NoOpConflictJudge()), memoryEmbedder: null);
}
