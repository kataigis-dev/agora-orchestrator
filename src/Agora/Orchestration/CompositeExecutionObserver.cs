namespace Agora.Orchestration;

/// <summary>Fans every execution event out to several observers, so presentation (e.g.
/// <see cref="ConsoleExecutionObserver"/>) and measurement (<see cref="MetricsExecutionObserver"/>)
/// can run side by side. Best-effort like any observer: it does not guard against member failures.</summary>
public sealed class CompositeExecutionObserver : IExecutionObserver
{
    private readonly IReadOnlyList<IExecutionObserver> _observers;

    /// <summary>Composes the given observers; events are delivered in order.</summary>
    public CompositeExecutionObserver(params IExecutionObserver[] observers) => _observers = observers;

    /// <inheritdoc />
    public void OnGraphStart(Graph graph) { foreach (var o in _observers) o.OnGraphStart(graph); }
    /// <inheritdoc />
    public void OnNodeStart(int step, string nodeId, string input) { foreach (var o in _observers) o.OnNodeStart(step, nodeId, input); }
    /// <inheritdoc />
    public void OnParallel(IReadOnlyList<string> branches) { foreach (var o in _observers) o.OnParallel(branches); }
    /// <inheritdoc />
    public void OnSignals(IReadOnlyCollection<string> signals) { foreach (var o in _observers) o.OnSignals(signals); }
    /// <inheritdoc />
    public void OnUsage(TokenUsage usage) { foreach (var o in _observers) o.OnUsage(usage); }
    /// <inheritdoc />
    public void OnArtifact(string key, string value) { foreach (var o in _observers) o.OnArtifact(key, value); }
    /// <inheritdoc />
    public void OnEdge(string label, string target, bool isEnd) { foreach (var o in _observers) o.OnEdge(label, target, isEnd); }
    /// <inheritdoc />
    public void OnGraphComplete() { foreach (var o in _observers) o.OnGraphComplete(); }
}
