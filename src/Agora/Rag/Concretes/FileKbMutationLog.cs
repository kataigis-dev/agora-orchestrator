using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agora.Rag.Contracts;
using Agora.Rag.Models;

namespace Agora.Rag.Concretes;

/// <summary>
/// Append-only, file-backed audit log of knowledge-base mutations, stored as JSON Lines (one record per
/// line) so a normal append never rewrites existing history and the log survives restarts. The vector
/// store still hard-deletes (retrieval stays tombstone-free); this is the separate audit surface.
/// Because it retains deleted content it has an explicit <see cref="PurgeAsync"/> path (the only
/// operation that rewrites the file) for retention / GDPR erasure.
/// </summary>
public sealed class FileKbMutationLog : IKbMutationLog
{
    private static readonly JsonSerializerOptions Json = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>Opens (lazily creates on first append) the log at <paramref name="path"/>.</summary>
    public FileKbMutationLog(string path) => _path = path;

    /// <summary>The default log path next to the config: <c>&lt;configDir&gt;/kb-mutations.jsonl</c>.</summary>
    public static string DefaultPath(string configDir) => Path.Combine(configDir, "kb-mutations.jsonl");

    /// <inheritdoc />
    public async Task AppendAsync(KbMutation mutation, CancellationToken cancellationToken = default)
    {
        var line = JsonSerializer.Serialize(mutation, Json) + "\n";
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(_path));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            await File.AppendAllTextAsync(_path, line, cancellationToken);
        }
        finally { _lock.Release(); }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KbMutation>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try { return ReadAllNoLock(); }
        finally { _lock.Release(); }
    }

    /// <inheritdoc />
    public async Task<int> PurgeAsync(Func<KbMutation, bool> remove, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var all = ReadAllNoLock();
            var kept = all.Where(m => !remove(m)).ToList();
            var removed = all.Count - kept.Count;
            if (removed == 0)
                return 0;

            var full = Path.GetFullPath(_path);
            var sb = new StringBuilder();
            foreach (var m in kept)
                sb.Append(JsonSerializer.Serialize(m, Json)).Append('\n');
            // Atomic replace (temp + move), as the vector store does, so a crash never corrupts the log.
            var temp = full + ".tmp";
            await File.WriteAllTextAsync(temp, sb.ToString(), cancellationToken);
            File.Move(temp, full, overwrite: true);
            return removed;
        }
        finally { _lock.Release(); }
    }

    private List<KbMutation> ReadAllNoLock()
    {
        if (!File.Exists(_path))
            return new();
        var result = new List<KbMutation>();
        foreach (var line in File.ReadAllLines(_path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            if (JsonSerializer.Deserialize<KbMutation>(line, Json) is { } m)
                result.Add(m);
        }
        return result;
    }
}
