namespace Agora.Rag;

/// <summary>Turns text into embedding vectors for similarity search.</summary>
public interface IEmbedder
{
    /// <summary>Embeds a batch of texts, returning one vector per input in order.</summary>
    Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
