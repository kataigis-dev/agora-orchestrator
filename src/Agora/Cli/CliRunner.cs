using System.Text.Json;
using Agora.Agents;
using Agora.Configuration;
using Agora.Eval;
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
        Agora.HumanInTheLoop.IApprovalHandler? approvalHandler = null,
        TextReader? @in = null,
        Agora.HumanInTheLoop.IConflictResolver? conflictResolver = null,
        Func<Configuration.VectorStoreConfig?, Agora.Rag.IVectorStore?>? storeResolver = null,
        Func<Agora.Rag.EmbedderSpec, Agora.Rag.IEmbedder?>? embedderResolver = null)
    {
        var stdout = @out ?? Console.Out;
        var stderr = error ?? Console.Error;
        if (args.Length == 0)
        {
            stderr.WriteLine("usage: agora <init|run|resume|ingest|validate|eval> [options]");
            stderr.WriteLine("  init     [--output <file>]   guided config builder");
            stderr.WriteLine("  run      --config <file> --input <text> [--agent <id>] [--graph] [--stream] [--checkpoint <dir>] [--run-id <id>]");
            stderr.WriteLine("  resume   --config <file> --checkpoint <dir> --run-id <id>");
            stderr.WriteLine("  eval     --config <file> --scenario <file.json>");
            return 1;
        }

        var command = args[0];
        var options = ParseOptions(args.Skip(1));

        if (command == "init")
            return ConfigWizard.Run(@in ?? Console.In, stdout, stderr,
                options.TryGetValue("output", out var path) ? path : null);

        if (command == "eval")
        {
            try
            {
                var scenarioPath = Require(options, "scenario");
                if (!File.Exists(scenarioPath))
                    throw new ConfigException($"scenario file not found: {scenarioPath}");
                var scenario = JsonSerializer.Deserialize<Scenario>(File.ReadAllText(scenarioPath),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new ConfigException("scenario file is empty or invalid");
                var result = ScenarioRunner.RunAsync(Require(options, "config"), scenario)
                    .GetAwaiter().GetResult();
                if (result.Passed)
                {
                    stdout.WriteLine("PASS");
                    return 0;
                }
                stderr.WriteLine("FAIL");
                foreach (var failure in result.Failures)
                    stderr.WriteLine($"  - {failure}");
                return 1;
            }
            catch (Exception e) when (e is ConfigException or GraphError or KeyNotFoundException or FileNotFoundException)
            {
                stderr.WriteLine($"ERROR: {e.Message}");
                return 1;
            }
        }

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
                var checkpoints = options.TryGetValue("checkpoint", out var cpDir)
                    ? new FileCheckpointStore(cpDir) : null;
                var stream = options.ContainsKey("stream");
                Action<string>? onChunk = stream ? chunk => stdout.Write(chunk) : null;
                var runtime = Runtime.FromConfig(Require(options, "config"),
                    provider ?? throw new InvalidOperationException("no chat provider supplied"),
                    toolAgentFactory: toolAgentFactory, approvalHandler: approvalHandler,
                    conflictResolver: conflictResolver, storeResolver: storeResolver,
                    embedderResolver: embedderResolver, checkpointStore: checkpoints);
                if (isGraph)
                {
                    var result = runtime.RunAsync(Require(options, "input"), options.GetValueOrDefault("run-id"), onChunk)
                        .GetAwaiter().GetResult();
                    if (stream) stdout.WriteLine(); else stdout.WriteLine(result.Output);
                    if (checkpoints is not null)
                        stderr.WriteLine($"run-id: {result.RunId}");
                }
                else
                {
                    var result = runtime.RunAgentAsync(Require(options, "agent"), Require(options, "input"), onChunk)
                        .GetAwaiter().GetResult();
                    if (stream) stdout.WriteLine(); else stdout.WriteLine(result.Output);
                }
                return 0;
            }
            catch (Exception e) when (e is ConfigException or GraphError or KeyNotFoundException or FileNotFoundException)
            {
                stderr.WriteLine($"ERROR: {e.Message}");
                return 1;
            }
        }

        if (command == "resume")
        {
            try
            {
                var runtime = Runtime.FromConfig(Require(options, "config"),
                    provider ?? throw new InvalidOperationException("no chat provider supplied"),
                    toolAgentFactory: toolAgentFactory, approvalHandler: approvalHandler,
                    conflictResolver: conflictResolver, storeResolver: storeResolver,
                    embedderResolver: embedderResolver,
                    checkpointStore: new FileCheckpointStore(Require(options, "checkpoint")));
                var result = runtime.ResumeAsync(Require(options, "run-id")).GetAwaiter().GetResult();
                stdout.WriteLine(result.Output);
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
                    provider ?? throw new InvalidOperationException("no chat provider supplied"),
                    storeResolver: storeResolver, embedderResolver: embedderResolver);
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
