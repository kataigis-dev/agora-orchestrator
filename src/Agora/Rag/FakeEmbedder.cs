using System.Security.Cryptography;
using System.Text;

namespace Agora.Rag;

/// <summary>
/// Deterministic bag-of-words embedder for tests and offline use. Uses MD5 (not the
/// process-randomized string.GetHashCode) so vectors are stable across runs.
/// </summary>
public sealed class FakeEmbedder : IEmbedder
{
    private readonly int _dim;

    /// <summary>Creates the embedder producing vectors of the given dimensionality.</summary>
    public FakeEmbedder(int dim = 32) => _dim = dim;

    /// <inheritdoc />
    public Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<float[]> vectors = texts.Select(Vector).ToList();
        return Task.FromResult(vectors);
    }

    /// <summary>Hashes each token into a bucket and counts occurrences to form a stable vector.</summary>
    private float[] Vector(string text)
    {
        var vec = new float[_dim];
        foreach (var token in text.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(token));
            var bucket = (int)(BitConverter.ToUInt64(hash, 0) % (ulong)_dim);
            vec[bucket] += 1.0f;
        }
        return vec;
    }
}
