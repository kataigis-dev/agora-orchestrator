using Microsoft.Extensions.AI;

namespace Agora.AgentFramework;

/// <summary>
/// Built-in filesystem tools (read_file, write_file, search_files, list_directory) that work
/// without an MCP server. Register alongside MCP tools in AgentFrameworkAgent.
/// </summary>
internal static class BuiltInFileTools
{
    /// <summary>Builds the subset of filesystem tools the agent allow-lists (read/write/search/list).</summary>
    public static List<AITool> Create(IReadOnlyList<string> allowedTools)
    {
        var set = allowedTools.ToHashSet(StringComparer.Ordinal);
        var tools = new List<AITool>();

        if (set.Contains("read_file"))
            tools.Add(AIFunctionFactory.Create(
                (string path) =>
                {
                    var full = Path.GetFullPath(path);
                    if (!File.Exists(full))
                        return $"error: file not found '{full}'";
                    return File.ReadAllText(full);
                },
                name: "read_file",
                description: "Read the full contents of a file at the given path. Returns the file content as text."));

        if (set.Contains("write_file"))
            tools.Add(AIFunctionFactory.Create(
                (string path, string content) =>
                {
                    var full = Path.GetFullPath(path);
                    var dir = Path.GetDirectoryName(full);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    File.WriteAllText(full, content);
                    return $"success: wrote {content.Length} bytes to '{full}'";
                },
                name: "write_file",
                description: "Write content to a file at the given path. Creates parent directories if needed."));

        if (set.Contains("search_files"))
            tools.Add(AIFunctionFactory.Create(
                (string pattern) =>
                {
                    var dir = Path.GetDirectoryName(pattern);
                    if (string.IsNullOrEmpty(dir)) dir = ".";
                    var search = Path.GetFileName(pattern);
                    if (!Directory.Exists(dir))
                        return $"error: directory not found '{dir}'";
                    var files = Directory.GetFiles(dir, search, SearchOption.AllDirectories);
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
                    var full = Path.GetFullPath(path);
                    if (!Directory.Exists(full))
                        return $"error: directory not found '{full}'";
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
}
