using System.Text.Json;
using Agora.AgentFramework;
using Microsoft.Extensions.AI;

namespace Agora.Tests.AgentFramework;

public class BuiltInFileToolsTests : IDisposable
{
    private readonly string _tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public BuiltInFileToolsTests() => Directory.CreateDirectory(_tmpDir);
    public void Dispose() { try { Directory.Delete(_tmpDir, true); } catch { } }

    private string Tmp(string name) => Path.Combine(_tmpDir, name);
    private static AIFunctionArguments Args(params (string Key, object? Value)[] items)
    {
        var args = new AIFunctionArguments();
        foreach (var (k, v) in items) args[k] = v;
        return args;
    }

    private static string AsString(object? result) =>
        result is JsonElement je ? je.GetString() ?? "" : result?.ToString() ?? "";

    [Fact]
    public async Task Create_ReadFile_ReturnsContent()
    {
        var path = Tmp("test.txt");
        File.WriteAllText(path, "hello world");
        var tools = BuiltInFileTools.Create(new[] { "read_file", "write_file", "search_files", "list_directory" });
        var readFile = tools.OfType<AIFunction>().First(t => t.Name == "read_file");
        var result = await readFile.InvokeAsync(Args(("path", path)));
        Assert.Equal("hello world", AsString(result));
    }

    [Fact]
    public async Task Create_ReadFile_Missing_ReturnsError()
    {
        var tools = BuiltInFileTools.Create(new[] { "read_file" });
        var readFile = tools.OfType<AIFunction>().First(t => t.Name == "read_file");
        var result = await readFile.InvokeAsync(Args(("path", Tmp("nonexistent.txt"))));
        Assert.Contains("error:", AsString(result).ToLowerInvariant());
    }

    [Fact]
    public async Task Create_WriteFile_CreatesFile()
    {
        var path = Tmp("out.txt");
        var tools = BuiltInFileTools.Create(new[] { "write_file" });
        var writeFile = tools.OfType<AIFunction>().First(t => t.Name == "write_file");
        await writeFile.InvokeAsync(Args(("path", path), ("content", "test content")));
        Assert.Equal("test content", File.ReadAllText(path));
    }

    [Fact]
    public async Task Create_WriteFile_CreatesParentDirectories()
    {
        var path = Tmp("sub/deep/out.txt");
        var tools = BuiltInFileTools.Create(new[] { "write_file" });
        var writeFile = tools.OfType<AIFunction>().First(t => t.Name == "write_file");
        await writeFile.InvokeAsync(Args(("path", path), ("content", "nested")));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task Create_SearchFiles_FindsPattern()
    {
        File.WriteAllText(Tmp("foo.cs"), "");
        File.WriteAllText(Tmp("bar.txt"), "");
        var tools = BuiltInFileTools.Create(new[] { "search_files" });
        var search = tools.OfType<AIFunction>().First(t => t.Name == "search_files");
        var result = await search.InvokeAsync(Args(("pattern", Path.Combine(_tmpDir, "*.cs"))));
        var output = AsString(result);
        Assert.Contains("foo.cs", output);
        Assert.DoesNotContain("bar.txt", output);
    }

    [Fact]
    public async Task Create_ListDirectory_ReturnsEntries()
    {
        File.WriteAllText(Tmp("file1.txt"), "");
        Directory.CreateDirectory(Tmp("subdir"));
        var tools = BuiltInFileTools.Create(new[] { "list_directory" });
        var listDir = tools.OfType<AIFunction>().First(t => t.Name == "list_directory");
        var result = await listDir.InvokeAsync(Args(("path", _tmpDir)));
        var output = AsString(result);
        Assert.Contains("file1.txt", output);
        Assert.Contains("subdir/", output);
    }

    [Fact]
    public void Create_OnlyAllows_ConfiguredTools()
    {
        var tools = BuiltInFileTools.Create(new[] { "read_file" });
        Assert.Contains(tools, t => t.Name == "read_file");
        Assert.DoesNotContain(tools, t => t.Name == "write_file");
        Assert.DoesNotContain(tools, t => t.Name == "search_files");
        Assert.DoesNotContain(tools, t => t.Name == "list_directory");
    }
}
