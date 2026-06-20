namespace Agora.Rag;

/// <summary>Vector similarity helpers used by in-process stores.</summary>
public static class VectorMath
{
    /// <summary>Cosine similarity of two vectors; returns 0 if either has zero magnitude. Compares over
    /// the shorter length when dimensions differ.</summary>
    public static double CosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        double dot = 0.0;
        var n = Math.Min(a.Count, b.Count);
        for (var i = 0; i < n; i++)
            dot += a[i] * b[i];

        double normA = 0.0;
        foreach (var x in a)
            normA += x * x;
        double normB = 0.0;
        foreach (var y in b)
            normB += y * y;

        if (normA == 0.0 || normB == 0.0)
            return 0.0;
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
