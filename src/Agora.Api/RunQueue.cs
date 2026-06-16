using System.Threading.Channels;

namespace Agora.Api;

/// <summary>Unbounded queue of run ids awaiting background execution.</summary>
public sealed class RunQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();

    public ValueTask EnqueueAsync(string runId) => _channel.Writer.WriteAsync(runId);
    public IAsyncEnumerable<string> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
