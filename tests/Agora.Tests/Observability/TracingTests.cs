using Agora.Observability;
using Xunit;

namespace Agora.Tests.Observability;

// Recording is process-wide global state; run this in a non-parallel collection so spans
// emitted by other tests (Runtime/graph runs) don't leak into the recorder mid-assert.
[CollectionDefinition("Tracing recorder", DisableParallelization = true)]
public sealed class TracingRecorderCollection;

[Collection("Tracing recorder")]
public class TracingTests
{
    [Fact]
    public void Span_RecordsNameAttributesAndDuration_WhenRecording()
    {
        var recorder = Tracing.EnableRecording();
        try
        {
            using (var span = Tracing.BeginSpan("agent.run", new() { ["agent"] = "writer" }))
            {
                span.Attributes["tokens"] = 5;
            }
            Assert.Single(recorder);
            Assert.Equal("agent.run", recorder[0].Name);
            Assert.Equal("writer", recorder[0].Attributes["agent"]);
            Assert.Equal(5, recorder[0].Attributes["tokens"]);
            Assert.True(recorder[0].DurationMs >= 0.0);
        }
        finally
        {
            Tracing.DisableRecording();
        }
    }

    [Fact]
    public void Span_IsNoOp_WhenRecordingDisabled()
    {
        Tracing.DisableRecording();
        using var span = Tracing.BeginSpan("noop");
        span.Attributes["x"] = 1;
        // no exception, nothing recorded
    }
}
