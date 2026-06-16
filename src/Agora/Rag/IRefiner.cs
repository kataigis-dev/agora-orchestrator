namespace Agora.Rag;

public interface IRefiner
{
    Task<RefinedQuery> RefineAsync(string text, CancellationToken cancellationToken = default);
}
