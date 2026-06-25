using System.Text.Json;
using Agora.AgentFramework.Agents;
using Agora.AgentFramework.Providers;
using Agora.AgentFramework.Rag;
using Agora.AgentFramework.Specs;
using Agora.AgentFramework.Mcp;
using Agora.AgentFramework.Tools;
using Microsoft.Extensions.AI;

namespace Agora.Tests.AgentFramework;

public class BuiltInFileToolsTests : IDisposable
{
    private readonly string _tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly string _outsideDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public BuiltInFileToolsTests()
    {
        Directory.CreateDirectory(_tmpDir);
        Directory.CreateDirectory(_outsideDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tmpDir, true); } catch { }
        try { Directory.Delete(_outsideDir, true); } catch { }
    }

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
        var tools = BuiltInFileTools.Create(_tmpDir, new[] { "read_file", "write_file", "search_files", "list_directory" });
        var readFile = tools.OfType<AIFunction>().First(t => t.Name == "read_file");
        var result = await readFile.InvokeAsync(Args(("path", path)));
        Assert.Equal("hello world", AsString(result));
    }

    [Fact]
    public async Task Create_ReadFile_Missing_ReturnsError()
    {
        var tools = BuiltInFileTools.Create(_tmpDir, new[] { "read_file" });
        var readFile = tools.OfType<AIFunction>().First(t => t.Name == "read_file");
        var result = await readFile.InvokeAsync(Args(("path", Tmp("nonexistent.txt"))));
        Assert.Contains("error:", AsString(result).ToLowerInvariant());
    }

    [Fact]
    public async Task Create_WriteFile_CreatesFile()
    {
        var path = Tmp("out.txt");
        var tools = BuiltInFileTools.Create(_tmpDir, new[] { "write_file" });
        var writeFile = tools.OfType<AIFunction>().First(t => t.Name == "write_file");
        await writeFile.InvokeAsync(Args(("path", path), ("content", "test content")));
        Assert.Equal("test content", File.ReadAllText(path));
    }

    [Fact]
    public async Task Create_WriteFile_CreatesParentDirectories()
    {
        var path = Tmp("sub/deep/out.txt");
        var tools = BuiltInFileTools.Create(_tmpDir, new[] { "write_file" });
        var writeFile = tools.OfType<AIFunction>().First(t => t.Name == "write_file");
        await writeFile.InvokeAsync(Args(("path", path), ("content", "nested")));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task Create_SearchFiles_FindsPattern()
    {
        File.WriteAllText(Tmp("foo.cs"), "");
        File.WriteAllText(Tmp("bar.txt"), "");
        var tools = BuiltInFileTools.Create(_tmpDir, new[] { "search_files" });
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
        var tools = BuiltInFileTools.Create(_tmpDir, new[] { "list_directory" });
        var listDir = tools.OfType<AIFunction>().First(t => t.Name == "list_directory");
        var result = await listDir.InvokeAsync(Args(("path", _tmpDir)));
        var output = AsString(result);
        Assert.Contains("file1.txt", output);
        Assert.Contains("subdir/", output);
    }

    [Fact]
    public void Create_OnlyAllows_ConfiguredTools()
    {
        var tools = BuiltInFileTools.Create(_tmpDir, new[] { "read_file" });
        Assert.Contains(tools, t => t.Name == "read_file");
        Assert.DoesNotContain(tools, t => t.Name == "write_file");
        Assert.DoesNotContain(tools, t => t.Name == "search_files");
        Assert.DoesNotContain(tools, t => t.Name == "list_directory");
    }

    [Fact]
    public async Task Create_ReadFile_AbsolutePathOutsideRoot_Rejected()
    {
        var secret = Path.Combine(_outsideDir, "secret.txt");
        File.WriteAllText(secret, "top secret");
        var tools = BuiltInFileTools.Create(_tmpDir, new[] { "read_file" });
        var readFile = tools.OfType<AIFunction>().First(t => t.Name == "read_file");
        var result = AsString(await readFile.InvokeAsync(Args(("path", secret))));
        Assert.Contains("outside the allowed workspace", result);
        Assert.DoesNotContain("top secret", result);
    }

    [Fact]
    public async Task Create_ReadFile_DotDotTraversal_Rejected()
    {
        var secret = Path.Combine(_outsideDir, "secret.txt");
        File.WriteAllText(secret, "top secret");
        // A relative path that climbs out of the sandbox into the sibling directory.
        var escape = Path.Combine("..", Path.GetFileName(_outsideDir), "secret.txt");
        var tools = BuiltInFileTools.Create(_tmpDir, new[] { "read_file" });
        var readFile = tools.OfType<AIFunction>().First(t => t.Name == "read_file");
        var result = AsString(await readFile.InvokeAsync(Args(("path", escape))));
        Assert.Contains("outside the allowed workspace", result);
        Assert.DoesNotContain("top secret", result);
    }

    [Fact]
    public async Task Create_WriteFile_OutsideRoot_DoesNotWrite()
    {
        var target = Path.Combine(_outsideDir, "planted.txt");
        var tools = BuiltInFileTools.Create(_tmpDir, new[] { "write_file" });
        var writeFile = tools.OfType<AIFunction>().First(t => t.Name == "write_file");
        var result = AsString(await writeFile.InvokeAsync(Args(("path", target), ("content", "x"))));
        Assert.Contains("outside the allowed workspace", result);
        Assert.False(File.Exists(target));
    }

    [Fact]
    public async Task Create_ListDirectory_OutsideRoot_Rejected()
    {
        var tools = BuiltInFileTools.Create(_tmpDir, new[] { "list_directory" });
        var listDir = tools.OfType<AIFunction>().First(t => t.Name == "list_directory");
        var result = AsString(await listDir.InvokeAsync(Args(("path", _outsideDir))));
        Assert.Contains("outside the allowed workspace", result);
    }

    [Fact]
    public async Task Create_WriteThenRead_RelativePath_ResolvesUnderRoot()
    {
        var tools = BuiltInFileTools.Create(_tmpDir, new[] { "write_file", "read_file" });
        var writeFile = tools.OfType<AIFunction>().First(t => t.Name == "write_file");
        var readFile = tools.OfType<AIFunction>().First(t => t.Name == "read_file");
        await writeFile.InvokeAsync(Args(("path", "nested/note.txt"), ("content", "inside")));
        Assert.Equal("inside", File.ReadAllText(Path.Combine(_tmpDir, "nested", "note.txt")));
        Assert.Equal("inside", AsString(await readFile.InvokeAsync(Args(("path", "nested/note.txt")))));
    }
}
