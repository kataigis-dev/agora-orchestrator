using System.Diagnostics;

namespace Agora.Observability;

/// <summary>A recorded span: a named, timed operation with attributes.</summary>
public sealed class SpanRecord
{
    /// <summary>Creates a span record with the given name.</summary>
    public SpanRecord(string name) => Name = name;

    /// <summary>The span name.</summary>
    public string Name { get; }

    /// <summary>Key/value attributes attached to the span.</summary>
    public Dictionary<string, object?> Attributes { get; } = new();

    /// <summary>Measured duration in milliseconds (set on dispose).</summary>
    public double DurationMs { get; set; }
}

/// <summary>Disposable timing scope: records the elapsed time when disposed.</summary>
public sealed class SpanScope : IDisposable
{
    private readonly SpanRecord _record;
    private readonly long _start;

    internal SpanScope(string name, IReadOnlyDictionary<string, object?>? attributes)
    {
        _record = new SpanRecord(name);
        if (attributes is not null)
            foreach (var kv in attributes)
                _record.Attributes[kv.Key] = kv.Value;
        _start = Stopwatch.GetTimestamp();
    }

    /// <summary>The span's attributes, mutable until the scope is disposed.</summary>
    public Dictionary<string, object?> Attributes => _record.Attributes;

    /// <summary>Stops timing and records the span.</summary>
    public void Dispose()
    {
        _record.DurationMs = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;
        Tracing.Record(_record);
    }
}

/// <summary>Minimal span recorder + logging skeleton (OpenTelemetry comes in a later phase).</summary>
public static class Tracing
{
    private static List<SpanRecord>? _recorder;

    /// <summary>Starts capturing spans into a list and returns it (used by tests).</summary>
    public static List<SpanRecord> EnableRecording()
    {
        _recorder = new List<SpanRecord>();
        return _recorder;
    }

    /// <summary>Stops capturing spans.</summary>
    public static void DisableRecording() => _recorder = null;

    /// <summary>Begins a timed span; dispose the returned scope to record it.</summary>
    public static SpanScope BeginSpan(string name, Dictionary<string, object?>? attributes = null)
        => new(name, attributes);

    /// <summary>Adds a completed span to the active recorder, if any.</summary>
    internal static void Record(SpanRecord record) => _recorder?.Add(record);
}
