using System.Diagnostics;

namespace Agora.Observability;

public sealed class SpanRecord
{
    public SpanRecord(string name) => Name = name;
    public string Name { get; }
    public Dictionary<string, object?> Attributes { get; } = new();
    public double DurationMs { get; set; }
}

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

    public Dictionary<string, object?> Attributes => _record.Attributes;

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

    public static List<SpanRecord> EnableRecording()
    {
        _recorder = new List<SpanRecord>();
        return _recorder;
    }

    public static void DisableRecording() => _recorder = null;

    public static SpanScope BeginSpan(string name, Dictionary<string, object?>? attributes = null)
        => new(name, attributes);

    internal static void Record(SpanRecord record) => _recorder?.Add(record);
}
