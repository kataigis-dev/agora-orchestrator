namespace Agora.Specs;

/// <summary>
/// File-backed spec store: persists the <see cref="SpecDocument"/> as canonical JSON on disk so the
/// specification is durable, diffable, and version-controllable. This is the default, deterministic
/// source of truth (no fuzzy retrieval involved).
/// </summary>
public sealed class FileSpecStore : ISpecStore
{
    private readonly string _path;

    /// <summary>Opens (or prepares) the store at <paramref name="path"/>.</summary>
    public FileSpecStore(string path) => _path = path;

    /// <inheritdoc />
    public Task<SpecDocument> LoadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(File.Exists(_path)
            ? SpecSerializer.Deserialize(File.ReadAllText(_path))
            : SpecDocument.Empty);

    /// <inheritdoc />
    public async Task SaveAsync(SpecDocument document, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(_path, SpecSerializer.Serialize(document), cancellationToken);
    }
}
