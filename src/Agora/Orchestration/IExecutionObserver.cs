namespace Agora.Orchestration;

/// <summary>Token usage from a single agent call, surfaced to observers for metrics. <see cref="CacheRead"/>
/// is the subset of <see cref="Input"/> served from the provider's prompt cache.</summary>
public readonly record struct TokenUsage(int Input, int Output, int CacheRead, int CacheWrite);

/// <summary>Receives execution events from <see cref="GraphExecutor"/> so presentation (console
/// rendering, structured logging, …) is decoupled from orchestration. Implementations are best-effort
/// and must not throw — the executor does not guard against observer failures.</summary>
public interface IExecutionObserver
{
    /// <summary>Raised after each agent call with the tokens it consumed (default no-op so existing
    /// observers need no change).</summary>
    void OnUsage(TokenUsage usage) { }

    /// <summary>Raised once before the run begins, with the graph about to be executed.</summary>
    void OnGraphStart(Graph graph);

    /// <summary>Raised when a node's agent is about to run (1-based <paramref name="step"/>).</summary>
    void OnNodeStart(int step, string nodeId, string input);

    /// <summary>Raised when a node fans out into concurrently-executing parallel branches.</summary>
    void OnParallel(IReadOnlyList<string> branches);

    /// <summary>Raised after a node runs, with the names of the signals it emitted (possibly empty).</summary>
    void OnSignals(IReadOnlyCollection<string> signals);

    /// <summary>Raised once per artifact a node produced.</summary>
    void OnArtifact(string key, string value);

    /// <summary>Raised when the executor takes an edge to <paramref name="target"/>
    /// (<paramref name="isEnd"/> is true when the target is END).</summary>
    void OnEdge(string label, string target, bool isEnd);

    /// <summary>Raised once after the run reaches END.</summary>
    void OnGraphComplete();
}

/// <summary>An <see cref="IExecutionObserver"/> that ignores every event (silent runs / tests).</summary>
public sealed class NullExecutionObserver : IExecutionObserver
{
    /// <summary>Shared, stateless instance.</summary>
    public static readonly NullExecutionObserver Instance = new();

    /// <inheritdoc />
    public void OnGraphStart(Graph graph) { }
    /// <inheritdoc />
    public void OnNodeStart(int step, string nodeId, string input) { }
    /// <inheritdoc />
    public void OnParallel(IReadOnlyList<string> branches) { }
    /// <inheritdoc />
    public void OnSignals(IReadOnlyCollection<string> signals) { }
    /// <inheritdoc />
    public void OnArtifact(string key, string value) { }
    /// <inheritdoc />
    public void OnEdge(string label, string target, bool isEnd) { }
    /// <inheritdoc />
    public void OnGraphComplete() { }
}
