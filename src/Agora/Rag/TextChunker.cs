namespace Agora.Rag;

public static class TextChunker
{
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
