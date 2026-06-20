using System.Threading.Channels;

namespace Agora.Api;

/// <summary>Unbounded queue of run ids awaiting background execution.</summary>
public sealed class RunQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();

    /// <summary>Enqueues a run id for background execution.</summary>
    public ValueTask EnqueueAsync(string runId) => _channel.Writer.WriteAsync(runId);

    /// <summary>Asynchronously yields queued run ids until cancellation.</summary>
    public IAsyncEnumerable<string> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
