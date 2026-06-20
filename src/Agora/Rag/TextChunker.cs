namespace Agora.Rag;

/// <summary>Splits text into fixed-size, overlapping character windows for ingestion.</summary>
public static class TextChunker
{
    /// <summary>Chunks <paramref name="text"/> into windows of <paramref name="chunkSize"/> characters
    /// advancing by <c>chunkSize - overlap</c> each step.</summary>
    /// <exception cref="ArgumentException">If <paramref name="chunkSize"/> is not positive.</exception>
    public static IReadOnlyList<string> Chunk(string text, int chunkSize, int overlap)
    {
        if (chunkSize <= 0)
            throw new ArgumentException("chunkSize must be positive", nameof(chunkSize));
        var step = Math.Max(1, chunkSize - overlap);
        var chunks = new List<string>();
        for (var i = 0; i < text.Length; i += step)
            chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
        return chunks;
    }
}
