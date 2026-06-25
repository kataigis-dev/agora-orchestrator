namespace Agora.Eval.Quality;

/// <summary>A scripted judge for offline tests: returns preset verdicts in order (or a fixed default),
/// recording each request for assertions. Makes no model calls.</summary>
public sealed class FakeJudge : IJudge
{
    private readonly Queue<JudgeVerdict> _verdicts;
    private readonly JudgeVerdict _default;

    /// <summary>Every request received, for test assertions (e.g. that a deterministic gate skipped the judge).</summary>
    public List<JudgeRequest> Requests { get; } = new();

    /// <summary>Creates the judge with a queue of scripted verdicts and a fallback default.</summary>
    public FakeJudge(IEnumerable<JudgeVerdict>? verdicts = null, JudgeVerdict? @default = null)
    {
        _verdicts = new Queue<JudgeVerdict>(verdicts ?? Enumerable.Empty<JudgeVerdict>());
        _default = @default ?? JudgeVerdict.Invalid("no scripted verdict");
    }

    /// <inheritdoc />
    public Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(_verdicts.Count > 0 ? _verdicts.Dequeue() : _default);
    }
}
