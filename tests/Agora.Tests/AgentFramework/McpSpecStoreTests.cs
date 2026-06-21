using Agora.AgentFramework.Agents;
using Agora.AgentFramework.Providers;
using Agora.AgentFramework.Rag;
using Agora.AgentFramework.Specs;
using Agora.AgentFramework.Mcp;
using Agora.AgentFramework.Tools;
using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Specs.Concretes;

namespace Agora.Tests.AgentFramework;

public class McpSpecStoreTests
{
    /// <summary>A fake RAG-over-MCP server: the write tool stores the payload, the read tool returns
    /// it wrapped in surrounding context (as a real semantic search would).</summary>
    private sealed class FakeInvoker : IMcpInvoker
    {
        private string? _stored;
        public int Writes { get; private set; }

        public Task<string> CallAsync(string tool, IReadOnlyDictionary<string, object?> args, CancellationToken ct)
        {
            if (tool == "rag_write")
            {
                _stored = args.Values.First()?.ToString();
                Writes++;
                return Task.FromResult("Added");
            }
            // read tool: emulate a search result that embeds the stored payload in extra text.
            return Task.FromResult(_stored is null ? "no relevant context found" : $"context:\n{_stored}\n---end");
        }
    }

    private static SpecStoreSpec Spec() =>
        new("mcp", "memory", Server: null, WriteTool: "rag_write", ReadTool: "rag_search",
            WriteArg: "text", QueryArg: "query", Key: "agora:spec-document");

    [Fact]
    public async Task SaveThenLoad_RoundTripsThroughMcp()
    {
        var invoker = new FakeInvoker();
        var store = new McpSpecStore(invoker, Spec());

        var doc = SpecDocument.Empty
            .WithRequirement(new Requirement
            {
                Id = "R1",
                Title = "Login",
                Status = RequirementStatus.Approved,
                AcceptanceCriteria = new[] { new AcceptanceCriterion("R1.A1", "session", new SpecCheck(CheckKind.Test, "test:auth")) },
            })
            .WithTask(new TaskItem { Id = "T1", Description = "do", RequirementIds = new[] { "R1" } });

        await store.SaveAsync(doc);
        Assert.Equal(1, invoker.Writes);

        var reloaded = await store.LoadAsync();
        Assert.Equal("Login", reloaded.FindRequirement("R1")!.Title);
        Assert.Equal(RequirementStatus.Approved, reloaded.FindRequirement("R1")!.Status);
        Assert.Equal(CheckKind.Test, reloaded.FindRequirement("R1")!.AcceptanceCriteria[0].Check.Kind);
        Assert.Equal(new[] { "R1" }, reloaded.FindTask("T1")!.RequirementIds);
    }

    [Fact]
    public async Task Load_BeforeAnyWrite_ReturnsEmpty()
    {
        var store = new McpSpecStore(new FakeInvoker(), Spec());
        var doc = await store.LoadAsync();
        Assert.Empty(doc.Requirements);
    }
}
