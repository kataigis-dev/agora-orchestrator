using Agora.Cli;
using Agora.HumanInTheLoop;
using Xunit;

namespace Agora.Tests.Cli;

public class ConsoleApprovalHandlerTests
{
    private static ApprovalRequest Req() =>
        new() { AgentId = "writer", FunctionName = "write_file", Arguments = "{\"path\":\"x\"}" };

    [Theory]
    [InlineData("y", true)]
    [InlineData("yes", true)]
    [InlineData("n", false)]
    [InlineData("", false)]
    public async Task RequestAsync_ReadsDecisionFromInput(string input, bool expected)
    {
        var handler = new ConsoleApprovalHandler(new StringReader(input + "\n"), new StringWriter());
        Assert.Equal(expected, await handler.RequestAsync(Req()));
    }

    [Fact]
    public async Task RequestAsync_PromptsWithFunctionName()
    {
        var output = new StringWriter();
        await new ConsoleApprovalHandler(new StringReader("n\n"), output).RequestAsync(Req());
        Assert.Contains("write_file", output.ToString());
    }
}
