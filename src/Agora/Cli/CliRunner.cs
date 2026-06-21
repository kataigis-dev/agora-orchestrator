using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using Agora.Configuration;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Specs.Concretes;

namespace Agora.Cli;

/// <summary>CLI entry point: parses arguments, selects the command, and runs it with the supplied
/// edge dependencies. Returns a process exit code.</summary>
public static class CliRunner
{
    /// <summary>Dispatches <paramref name="args"/> to the matching command and returns its exit code.
    /// I/O streams default to the console but can be injected (used by tests).</summary>
    public static int Run(
        string[] args,
        IChatProvider? provider = null,
        TextWriter? @out = null,
        TextWriter? error = null,
        TextReader? @in = null,
        IToolAgentFactory? toolAgentFactory = null,
        HumanInTheLoop.IApprovalHandler? approvalHandler = null,
        HumanInTheLoop.IConflictResolver? conflictResolver = null,
        Func<VectorStoreConfig?, IVectorStore?>? storeResolver = null,
        Func<EmbedderSpec, IEmbedder?>? embedderResolver = null,
        Func<SpecStoreSpec, ISpecStore?>? specStoreResolver = null)
    {
        var action = args.ElementAtOrDefault(0);
        var options = ParseOptions(args?.Skip(1));
        var state = new ConfigState(options, provider, toolAgentFactory,
            approvalHandler, conflictResolver, storeResolver, embedderResolver, specStoreResolver,
            @in ?? Console.In, @out ?? Console.Out, error ?? Console.Error);
        var command = action switch
        {
            "init" => CommandStrategy.Init,
            "eval" => CommandStrategy.Eval,
            "validate" => CommandStrategy.Validate,
            "run" => CommandStrategy.Run,
            "resume" => CommandStrategy.Resume,
            "ingest" => CommandStrategy.Ingest,
            _ => CommandStrategy.EmptyArgs
        };

        return CommandStrategy.Execute(command, state);
    }

    /// <summary>Parses <c>--key value</c> / <c>--flag</c> arguments into a map (flags become "true").</summary>
    private static Dictionary<string, string> ParseOptions(IEnumerable<string>? args)
    {
        if (args?.Count() is null or 0) return [];

        var list = args.ToList();
        var options = new Dictionary<string, string>();
        for (var i = 0; i < list.Count; i++)
        {
            if (!list[i].StartsWith("--", StringComparison.Ordinal))
                continue;
            var key = list[i][2..];
            if (i + 1 < list.Count && !list[i + 1].StartsWith("--", StringComparison.Ordinal))
                options[key] = list[++i];
            else
                options[key] = "true";
        }
        return options;
    }
}
