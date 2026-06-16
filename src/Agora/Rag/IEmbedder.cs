namespace Agora.Rag;

public interface IEmbedder
{
    Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
