using System.Diagnostics;

namespace Agora.Orchestration;

/// <summary>An <see cref="IExecutionObserver"/> that aggregates run events into <see cref="RunMetrics"/>
/// without rendering anything — the measurement layer for comparing configurations (gate value, cost,
/// rework, cache effectiveness). Compose it with a presentation observer via
/// <see cref="CompositeExecutionObserver"/>; read <see cref="Metrics"/> after the run.</summary>
public sealed class MetricsExecutionObserver : IExecutionObserver
{
    private readonly Stopwatch _stopwatch = new();
    private readonly Dictionary<string, int> _nodeVisits = new();
    private readonly Dictionary<string, int> _signals = new();
    private int _steps;
    private int _parallelForks;
    private int _input, _output, _cacheRead, _cacheWrite;
    private bool _completed;

    /// <summary>The metrics collected so far (or final, once the run completes). Safe to read even if
    /// the run aborted — <see cref="RunMetrics.Completed"/> stays false and the duration reflects the
    /// elapsed time.</summary>
    public RunMetrics Metrics => new()
    {
        Steps = _steps,
        Completed = _completed,
        Duration = _stopwatch.Elapsed,
        NodeVisits = new Dictionary<string, int>(_nodeVisits),
        ReworkCount = _nodeVisits.Values.Sum(v => v - 1),
        ParallelForks = _parallelForks,
        Signals = new Dictionary<string, int>(_signals),
        InputTokens = _input,
        OutputTokens = _output,
        CacheReadTokens = _cacheRead,
        CacheWriteTokens = _cacheWrite,
    };

    /// <inheritdoc />
    public void OnGraphStart(Graph graph) => _stopwatch.Restart();

    /// <inheritdoc />
    public void OnNodeStart(int step, string nodeId, string input)
    {
        _steps = Math.Max(_steps, step);
        Visit(nodeId);
    }

    /// <inheritdoc />
    public void OnParallel(IReadOnlyList<string> branches)
    {
        _parallelForks++;
        foreach (var branch in branches)
            Visit(branch);
    }

    /// <inheritdoc />
    public void OnSignals(IReadOnlyCollection<string> signals)
    {
        foreach (var signal in signals)
            _signals[signal] = _signals.GetValueOrDefault(signal) + 1;
    }

    /// <inheritdoc />
    public void OnUsage(TokenUsage usage)
    {
        _input += usage.Input;
        _output += usage.Output;
        _cacheRead += usage.CacheRead;
        _cacheWrite += usage.CacheWrite;
    }

    /// <inheritdoc />
    public void OnArtifact(string key, string value) { }

    /// <inheritdoc />
    public void OnEdge(string label, string target, bool isEnd) { }

    /// <inheritdoc />
    public void OnGraphComplete()
    {
        _stopwatch.Stop();
        _completed = true;
    }

    private void Visit(string nodeId) => _nodeVisits[nodeId] = _nodeVisits.GetValueOrDefault(nodeId) + 1;
}
