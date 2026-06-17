using Agora.Agents;
using Agora.Configuration;
using Agora.Orchestration;
using Agora.Providers;
using Agora.Rag;

namespace Agora.Cli;

public static class CliRunner
{
    public static int Run(
        string[] args,
        IChatProvider? provider = null,
        TextWriter? @out = null,
        TextWriter? error = null,
        IToolAgentFactory? toolAgentFactory = null,
        Agora.HumanInTheLoop.IApprovalHandler? approvalHandler = null)
    {
        var stdout = @out ?? Console.Out;
        var stderr = error ?? Console.Error;
        if (args.Length == 0)
        {
            stderr.WriteLine("usage: agora <run|ingest|validate> [options]");
            stderr.WriteLine("  run      --config <file> --input <text> [--agent <id>] [--graph]");
            return 1;
        }

        var command = args[0];
        var options = ParseOptions(args.Skip(1));

        if (command == "validate")
        {
            try { ConfigLoader.Load(Require(options, "config")); }
            catch (ConfigException e) { stderr.WriteLine($"INVALID: {e.Message}"); return 1; }
            stdout.WriteLine("OK");
            return 0;
        }

        if (command == "run")
        {
            try
            {
                var isGraph = options.ContainsKey("graph");
                var runtime = Runtime.FromConfig(Require(options, "config"),
                    provider ?? throw new InvalidOperationException("no chat provider supplied"),
                    toolAgentFactory: toolAgentFactory, approvalHandler: approvalHandler);
                if (isGraph)
                {
                    var result = runtime.RunAsync(Require(options, "input")).GetAwaiter().GetResult();
                    stdout.WriteLine(result.Output);
                }
                else
                {
                    var result = runtime.RunAgentAsync(Require(options, "agent"), Require(options, "input"))
                        .GetAwaiter().GetResult();
                    stdout.WriteLine(result.Output);
                }
                return 0;
            }
            catch (Exception e) when (e is ConfigException or GraphError or KeyNotFoundException or FileNotFoundException)
            {
                stderr.WriteLine($"ERROR: {e.Message}");
                return 1;
            }
        }

        if (command == "ingest")
        {
            try
            {
                var runtime = Runtime.FromConfig(Require(options, "config"),
                    provider ?? throw new InvalidOperationException("no chat provider supplied"));
                if (runtime.Rag is null)
                {
                    stderr.WriteLine("ERROR: config has no enabled 'rag' section");
                    return 1;
                }
                var ingestCfg = runtime.Config.Rag?.Ingest;
                var ingestor = new Ingestor(runtime.Rag.Embedder, runtime.Rag.Store,
                    chunkSize: ingestCfg?.ChunkSize ?? 800, overlap: ingestCfg?.ChunkOverlap ?? 120);
                var count = ingestor.IngestPathsAsync(ingestCfg?.Sources ?? new List<string>())
                    .GetAwaiter().GetResult();
                stdout.WriteLine($"ingested {count} chunks");
                return 0;
            }
            catch (Exception e) when (e is ConfigException or GraphError or KeyNotFoundException or FileNotFoundException)
            {
                stderr.WriteLine($"ERROR: {e.Message}");
                return 1;
            }
        }

        stderr.WriteLine($"unknown command '{command}'");
        return 1;
    }

    private static Dictionary<string, string> ParseOptions(IEnumerable<string> args)
    {
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

    private static string Require(Dictionary<string, string> options, string name)
        => options.TryGetValue(name, out var value)
            ? value
            : throw new ConfigException($"missing required option --{name}");
}
