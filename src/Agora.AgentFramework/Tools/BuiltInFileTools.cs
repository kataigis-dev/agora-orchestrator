using Agora.AgentFramework.Tools;
using Agora.AgentFramework.Mcp;
using Agora.AgentFramework.Specs;
using Agora.AgentFramework.Rag;
using Agora.AgentFramework.Providers;
using Agora.AgentFramework.Agents;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework.Tools;

/// <summary>
/// Built-in filesystem tools (read_file, write_file, search_files, list_directory) that work
/// without an MCP server. Register alongside MCP tools in AgentFrameworkAgent.
///
/// Every path is <b>sandboxed</b> to a single root directory (the config directory by default):
/// supplied paths are resolved against the root and any path that escapes it — via <c>..</c>
/// traversal, an absolute path, or a different drive — is rejected before any file access. This
/// keeps a non-deterministic agent (or a prompt-injection payload) from reading or writing arbitrary
/// files outside the workspace. The check is path-normalization based; it does not resolve symlinks,
/// so a symlink already present inside the root that points outside is not blocked.
/// </summary>
internal static class BuiltInFileTools
{
    /// <summary>Builds the subset of filesystem tools the agent allow-lists (read/write/search/list),
    /// each confined to <paramref name="root"/> (empty = the current working directory).</summary>
    public static List<AITool> Create(string root, IReadOnlyList<string> allowedTools)
    {
        var rootFull = Path.GetFullPath(string.IsNullOrEmpty(root) ? Directory.GetCurrentDirectory() : root);
        var set = allowedTools.ToHashSet(StringComparer.Ordinal);
        var tools = new List<AITool>();

        if (set.Contains("read_file"))
            tools.Add(AIFunctionFactory.Create(
                (string path) =>
                {
                    if (!TryResolve(rootFull, path, out var full))
                        return OutsideRoot(path);
                    if (!File.Exists(full))
                        return $"error: file not found '{path}'";
                    return File.ReadAllText(full);
                },
                name: "read_file",
                description: "Read the full contents of a file at the given path. Returns the file content as text."));

        if (set.Contains("write_file"))
            tools.Add(AIFunctionFactory.Create(
                (string path, string content) =>
                {
                    if (!TryResolve(rootFull, path, out var full))
                        return OutsideRoot(path);
                    var dir = Path.GetDirectoryName(full);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    File.WriteAllText(full, content);
                    return $"success: wrote {content.Length} bytes to '{path}'";
                },
                name: "write_file",
                description: "Write content to a file at the given path. Creates parent directories if needed."));

        if (set.Contains("search_files"))
            tools.Add(AIFunctionFactory.Create(
                (string pattern) =>
                {
                    var dir = Path.GetDirectoryName(pattern);
                    if (string.IsNullOrEmpty(dir)) dir = ".";
                    if (!TryResolve(rootFull, dir, out var dirFull))
                        return OutsideRoot(pattern);
                    var search = Path.GetFileName(pattern);
                    if (!Directory.Exists(dirFull))
                        return $"error: directory not found '{dir}'";
                    var files = Directory.GetFiles(dirFull, search, SearchOption.AllDirectories);
                    if (files.Length == 0)
                        return "no files found";
                    return string.Join("\n", files.Select(f => f));
                },
                name: "search_files",
                description: "Recursively search for files matching a glob pattern (e.g. 'src/**/*.cs'). Returns matching paths, one per line."));

        if (set.Contains("list_directory"))
            tools.Add(AIFunctionFactory.Create(
                (string path) =>
                {
                    if (!TryResolve(rootFull, path, out var full))
                        return OutsideRoot(path);
                    if (!Directory.Exists(full))
                        return $"error: directory not found '{path}'";
                    var entries = Directory.GetFileSystemEntries(full)
                        .Select(e =>
                        {
                            var name = Path.GetFileName(e);
                            return Directory.Exists(e) ? $"{name}/" : name;
                        });
                    return string.Join("\n", entries);
                },
                name: "list_directory",
                description: "List files and directories in the given path. Directories are suffixed with '/'."));

        return tools;
    }

    /// <summary>Resolves <paramref name="path"/> against <paramref name="rootFull"/> and confirms the
    /// result stays inside the root. Returns false (with <paramref name="full"/> unset) when the path
    /// escapes the root via <c>..</c>, an absolute path, or a different drive.</summary>
    private static bool TryResolve(string rootFull, string path, out string full)
    {
        full = Path.GetFullPath(path, rootFull);
        var relative = Path.GetRelativePath(rootFull, full);
        if (relative == ".."
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || Path.IsPathRooted(relative))
        {
            full = string.Empty;
            return false;
        }
        return true;
    }

    private static string OutsideRoot(string path) => $"error: path '{path}' is outside the allowed workspace";
}
