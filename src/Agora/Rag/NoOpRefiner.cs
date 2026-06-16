namespace Agora.Rag;

public sealed class NoOpRefiner : IRefiner
{
    public Task<RefinedQuery> RefineAsync(string text, CancellationToken cancellationToken = default)
        => Task.FromResult(new RefinedQuery { Query = text });
}
