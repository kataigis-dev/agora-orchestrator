using Agora.HumanInTheLoop;

namespace Agora.Cli;

/// <summary>Prompts a human on the console to resolve a knowledge-base conflict.</summary>
public sealed class ConsoleConflictResolver : IConflictResolver
{
    private readonly TextReader _input;
    private readonly TextWriter _output;

    /// <summary>Creates the resolver, defaulting to <see cref="Console.In"/> and <see cref="Console.Error"/>.</summary>
    public ConsoleConflictResolver(TextReader? input = null, TextWriter? output = null)
    {
        _input = input ?? Console.In;
        _output = output ?? Console.Error;
    }

    /// <summary>Prints the conflict and prompts the user to keep existing, keep new, or merge.</summary>
    public Task<ConflictDecision> ResolveAsync(
        ConflictResolutionRequest request, CancellationToken cancellationToken = default)
    {
        _output.WriteLine($"[conflict] agent '{request.AgentId}' wants to record:");
        _output.WriteLine($"  NEW: {request.NewEntry}");
        _output.WriteLine("  conflicts with existing:");
        foreach (var existing in request.ExistingEntries)
            _output.WriteLine($"    - {existing}");
        if (request.Explanation.Length > 0)
            _output.WriteLine($"  reason: {request.Explanation}");
        if (request.SuggestedMerge.Length > 0)
            _output.WriteLine($"  judge's suggested merge: {request.SuggestedMerge}");

        _output.Write("resolve? [e]xisting / [n]ew / [m]erge: ");
        var choice = _input.ReadLine()?.Trim().ToLowerInvariant() ?? "";

        if (choice.StartsWith("n"))
            return Task.FromResult(new ConflictDecision { Resolution = ConflictResolution.KeepNew });
        if (choice.StartsWith("m"))
        {
            // Offer the judge's suggestion as the default: an empty line accepts it.
            _output.Write(request.SuggestedMerge.Length > 0
                ? "merged text [Enter = accept suggestion]: "
                : "merged text: ");
            var entered = _input.ReadLine()?.Trim() ?? "";
            var merged = entered.Length > 0 ? entered : request.SuggestedMerge;
            return Task.FromResult(new ConflictDecision
            {
                Resolution = ConflictResolution.Merge,
                MergedText = merged,
            });
        }
        return Task.FromResult(new ConflictDecision { Resolution = ConflictResolution.KeepExisting });
    }
}
