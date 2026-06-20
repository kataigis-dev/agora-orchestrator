using Agora.HumanInTheLoop;

namespace Agora.Cli;

/// <summary>Prompts a human on the console (stderr) to approve or reject a tool call.</summary>
public sealed class ConsoleApprovalHandler : IApprovalHandler
{
    private readonly TextReader _input;
    private readonly TextWriter _output;

    /// <summary>Creates the handler, defaulting to <see cref="Console.In"/> and <see cref="Console.Error"/>.</summary>
    public ConsoleApprovalHandler(TextReader? input = null, TextWriter? output = null)
    {
        _input = input ?? Console.In;
        _output = output ?? Console.Error;
    }

    /// <summary>Prints the request and returns true only if the user answers yes.</summary>
    public Task<bool> RequestAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        _output.WriteLine($"[approval] agent '{request.AgentId}' wants to call {request.FunctionName}({request.Arguments})");
        _output.Write("approve? [y/N]: ");
        var line = _input.ReadLine();
        var approved = line?.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase) == true;
        return Task.FromResult(approved);
    }
}
