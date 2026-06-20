using System.Text.Json;
using Agora.Configuration;
using Agora.Eval;
using Agora.Orchestration;
using Agora.Rag;

namespace Agora.Cli;

internal static class CommandStrategy
{
    public static Func<ConfigState, int> EmptyArgs => (_) =>
    {
        Console.Error.WriteLine("usage: agora <init|run|resume|ingest|validate|eval> [options]");
        Console.Error.WriteLine("  init     [--output <file>]   guided config builder");
        Console.Error.WriteLine("  run      --config <file> --input <text> [--agent <id>] [--graph] [--stream] [--checkpoint <dir>] [--run-id <id>]");
        Console.Error.WriteLine("  resume   --config <file> --checkpoint <dir> --run-id <id>");
        Console.Error.WriteLine("  eval     --config <file> --scenario <file.json>");
        return 1;
    };

    public static Func<ConfigState, int> Init => (state) =>
    {
        return ConfigWizard.Run(Console.In, Console.Out, Console.Error,
                state.Options.TryGetValue("output", out var path) ? path : null);
    };

    public static Func<ConfigState, int> Eval => (state) =>
    {
        var scenarioPath = Require(state.Options, "scenario");
        if (!File.Exists(scenarioPath))
            throw new ConfigException($"scenario file not found: {scenarioPath}");
        var scenario = JsonSerializer.Deserialize<Scenario>(
            File.ReadAllText(scenarioPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new ConfigException("scenario file is empty or invalid");
        var result = ScenarioRunner.RunAsync(Require(state.Options, "config"), scenario)
            .GetAwaiter().GetResult();
        if (result.Passed)
        {
            Console.Out.WriteLine("PASS");
            return 0;
        }
        Console.Error.WriteLine("FAIL");
        foreach (var failure in result.Failures)
            Console.Error.WriteLine($"  - {failure}");
        return 1;
    };

    public static Func<ConfigState, int> Validate => (state) =>
    {
        ConfigLoader.Load(Require(state.Options, "config"));
        Console.Out.WriteLine("OK");
        return 0;
    };

    public static Func<ConfigState, int> Run => (state) =>
    {
        var isGraph = state.Options.ContainsKey("graph");
        var checkpoints = state.Options.TryGetValue("checkpoint", out var cpDir)
            ? new FileCheckpointStore(cpDir) : null;
        var stream = state.Options.ContainsKey("stream");
        Action<string>? onChunk = stream ? chunk => Console.Out.Write(chunk) : null;
        var runtime = Runtime.FromConfig(Require(state.Options, "config"),
            state.Provider ?? throw new InvalidOperationException("no chat provider supplied"),
            toolAgentFactory: state.ToolAgentFactory, approvalHandler: state.ApprovalHandler,
            conflictResolver: state.ConflictResolver, storeResolver: state.StoreResolver,
            embedderResolver: state.EmbedderResolver, checkpointStore: checkpoints);
        if (isGraph)
        {
            var result = runtime.RunAsync(Require(state.Options, "input"), state.Options.GetValueOrDefault("run-id"), onChunk)
                .GetAwaiter().GetResult();
            if (stream) Console.Out.WriteLine(); else Console.Out.WriteLine(result.Output);
            if (checkpoints is not null)
                Console.Error.WriteLine($"run-id: {result.RunId}");
        }
        else
        {
            var result = runtime.RunAgentAsync(Require(state.Options, "agent"), Require(state.Options, "input"), onChunk)
                .GetAwaiter().GetResult();
            if (stream) Console.Out.WriteLine(); else Console.Out.WriteLine(result.Output);
        }
        return 0;
    };

    public static Func<ConfigState, int> Resume = (state) =>
    {
        var runtime = Runtime.FromConfig(Require(state.Options, "config"),
            state.Provider ?? throw new InvalidOperationException("no chat provider supplied"),
            toolAgentFactory: state.ToolAgentFactory, approvalHandler: state.ApprovalHandler,
            conflictResolver: state.ConflictResolver, storeResolver: state.StoreResolver,
            embedderResolver: state.EmbedderResolver,
            checkpointStore: new FileCheckpointStore(Require(state.Options, "checkpoint")));
        var result = runtime.ResumeAsync(Require(state.Options, "run-id")).GetAwaiter().GetResult();
        Console.Out.WriteLine(result.Output);
        return 0;
    };

    public static Func<ConfigState, int> Ingest => (state) =>
    {
        var runtime = Runtime.FromConfig(Require(state.Options, "config"),
            state.Provider ?? throw new InvalidOperationException("no chat provider supplied"),
            storeResolver: state.StoreResolver, embedderResolver: state.EmbedderResolver);
        if (runtime.Rag is null)
        {
            Console.Error.WriteLine("ERROR: config has no enabled 'rag' section");
            return 1;
        }
        var ingestCfg = runtime.Config.Rag?.Ingest;
        var ingestor = new Ingestor(runtime.Rag.Embedder, runtime.Rag.Store,
            chunkSize: ingestCfg?.ChunkSize ?? 800, overlap: ingestCfg?.ChunkOverlap ?? 120);
        var count = ingestor.IngestPathsAsync(ingestCfg?.Sources ?? new List<string>())
            .GetAwaiter().GetResult();
        Console.Out.WriteLine($"ingested {count} chunks");
        return 0;
    };

    public static int Execute(Func<ConfigState, int> command, ConfigState state)
    {
        try
        {
            return command(state);
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }
    }

    private static string Require(Dictionary<string, string> options, string name)
        => options.TryGetValue(name, out var value)
            ? value
            : throw new ConfigException($"missing required option --{name}");
}