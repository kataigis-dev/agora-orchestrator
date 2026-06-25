using Agora.AgentFramework.Mcp;
using Xunit;

namespace Agora.Tests.AgentFramework;

public class RagMcpStdioServerTests
{
    [Fact]
    public void BuildTools_ExposesOnlyReadOnlyRagSearch()
    {
        var tools = RagMcpStdioServer.BuildTools((query, ct) => Task.FromResult($"ctx:{query}"));

        // Exactly one tool, named rag_search; no write/mutation tool is ever exposed.
        Assert.Single(tools);
        Assert.Equal(new[] { "rag_search" }, tools.PrimitiveNames.ToArray());
        Assert.DoesNotContain(tools.PrimitiveNames, n => n.Contains("write"));
    }
}
