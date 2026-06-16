using Agora.Orchestration;
using Xunit;

namespace Agora.Tests.Orchestration;

public class StateTests
{
    [Fact]
    public void State_Defaults()
    {
        var s = new State("hi");
        Assert.Equal("hi", s.UserInput);
        Assert.Empty(s.Messages);
        Assert.Empty(s.Outputs);
        Assert.Empty(s.Signals);
        Assert.Empty(s.LoopCounters);
        Assert.Null(s.LastAgent);
    }

    [Fact]
    public void Inbox_ReturnsOnlyMessagesForRecipient()
    {
        var s = new State("hi");
        s.Messages.Add(new Message("planner", "writer", "PLAN"));
        s.Messages.Add(new Message("x", "other", "NOPE"));
        s.Messages.Add(new Message("y", "writer", "MORE"));
        Assert.Equal("PLAN\n\nMORE", s.Inbox("writer"));
        Assert.Equal("", s.Inbox("nobody"));
    }
}
