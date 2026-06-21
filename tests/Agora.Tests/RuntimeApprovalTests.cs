using Agora;
using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using Agora.HumanInTheLoop;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Xunit;

namespace Agora.Tests;

public class RuntimeApprovalTests
{
    private sealed class CapturingFactory : IToolAgentFactory
    {
        public AgentBuildContext? Last { get; private set; }
        public IAgent Create(AgentBuildContext context)
        {
            Last = context;
            return new Stub();
        }
        private sealed class Stub : IAgent
        {
            public Task<AgentResult> RunAsync(string userInput, string context = "", Action<string>? onChunk = null)
                => Task.FromResult(new AgentResult { Output = "stub" });
        }
    }

    private const string Config = """
        providers:
          openai: { api_key_env: OPENAI_API_KEY }
        models:
          balanced: { provider: openai, model: gpt-4o }
        agents:
          writer:
            model: balanced
            tools: [read_file, write_file]
            approvals: [write_file]
        """;

    private static string WriteConfig()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, Config);
        return path;
    }

    [Fact]
    public void BuildAgent_ThreadsApprovalsAndHandlerIntoContext()
    {
        var factory = new CapturingFactory();
        var handler = new FakeApprovalHandler();
        var runtime = Runtime.FromConfig(WriteConfig(), new FakeChatProvider(), toolAgentFactory: factory, approvalHandler: handler);

        runtime.BuildAgent("writer");

        Assert.Equal(new[] { "write_file" }, factory.Last!.Approvals);
        Assert.Same(handler, factory.Last.ApprovalHandler);
    }

    [Fact]
    public void BuildAgent_ApprovalsDeclared_NoHandler_Throws()
    {
        var runtime = Runtime.FromConfig(WriteConfig(), new FakeChatProvider(), toolAgentFactory: new CapturingFactory());
        var ex = Assert.Throws<InvalidOperationException>(() => runtime.BuildAgent("writer"));
        Assert.Contains("IApprovalHandler", ex.Message);
    }
}
