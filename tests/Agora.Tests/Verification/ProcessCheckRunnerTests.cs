using Agora.Configuration;
using Agora.Verification.Contracts;
using Agora.Verification.Models;
using Agora.Verification.Concretes;

namespace Agora.Tests.Verification;

public class ProcessCheckRunnerTests
{
    private static ChecksConfig Config(params (string Name, string Command, string[] Args)[] checks)
    {
        var cfg = new ChecksConfig { Timeout = 60 };
        foreach (var (name, command, args) in checks)
            cfg.Commands[name] = new CheckCommandConfig { Command = command, Args = args.ToList() };
        return cfg;
    }

    [Fact]
    public async Task SuccessfulCommand_Passes()
    {
        var runner = new ProcessCheckRunner(Config(("ver", "dotnet", new[] { "--version" })));
        var result = await runner.RunAsync("ver", new Dictionary<string, string>());

        Assert.True(result.Passed);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task FailingCommand_DoesNotPass()
    {
        var runner = new ProcessCheckRunner(Config(("bad", "dotnet", new[] { "--this-flag-does-not-exist" })));
        var result = await runner.RunAsync("bad", new Dictionary<string, string>());

        Assert.False(result.Passed);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task UnknownCheck_IsReportedNotThrown()
    {
        var runner = new ProcessCheckRunner(Config());
        var result = await runner.RunAsync("nope", new Dictionary<string, string>());

        Assert.False(result.Passed);
        Assert.Equal(-1, result.ExitCode);
        Assert.Contains("unknown check", result.Output);
        Assert.False(runner.HasCheck("nope"));
    }

    [Fact]
    public async Task SubstitutesPlaceholder_AsASingleArgvEntry()
    {
        // The whole flag is a {placeholder}: substitution makes it `dotnet --version` (exit 0);
        // without substitution the literal "{flag}" reaches dotnet and errors — proving the value is
        // passed as one argv token through no shell.
        var runner = new ProcessCheckRunner(Config(("sub", "dotnet", new[] { "{flag}" })));

        var substituted = await runner.RunAsync("sub", new Dictionary<string, string> { ["flag"] = "--version" });
        Assert.True(substituted.Passed);

        var literal = await runner.RunAsync("sub", new Dictionary<string, string>());
        Assert.False(literal.Passed);
    }
}
