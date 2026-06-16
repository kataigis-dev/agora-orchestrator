namespace Agora.Rag;

public sealed record Chunk(string Text, string Source, double Score = 0.0);
