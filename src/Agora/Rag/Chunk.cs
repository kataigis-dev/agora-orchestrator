namespace Agora.Rag;

/// <summary>A unit of stored text. <see cref="Id"/> is the store-assigned stable
/// identity (empty until persisted) used for update/delete; <see cref="Score"/> is
/// the transient similarity from the last query.</summary>
public sealed record Chunk(string Text, string Source, double Score = 0.0, string Id = "");
