using Agora.Agents;
using Agora.Configuration;
using Agora.Providers;
using Agora.Rag;

namespace Agora.Cli;

public static class CliRunner
{
    public static int Run(
        string[] args,
        IChatProvider? provider = null,
        IToolAgentFactory? toolAgentFactory = null,
        HumanInTheLoop.IApprovalHandler? approvalHandler = null,
        HumanInTheLoop.IConflictResolver? conflictResolver = null,
        Func<VectorStoreConfig?, IVectorStore?>? storeResolver = null,
        Func<EmbedderSpec, IEmbedder?>? embedderResolver = null)
    {
        var action = args.ElementAtOrDefault(0);
        var options = ParseOptions(args?.Skip(1));
        var state = new ConfigState(options, provider, toolAgentFactory, 
            approvalHandler, conflictResolver, storeResolver, embedderResolver);
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
