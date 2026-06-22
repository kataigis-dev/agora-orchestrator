using System.Text.Json;
using Agora.Configuration;
using Agora.Eval;
using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;

namespace Agora.Cli;

/// <summary>The handlers behind each CLI verb (<c>init</c>/<c>run</c>/<c>resume</c>/<c>ingest</c>/
/// <c>validate</c>/<c>eval</c>), each a <c>ConfigState → exit code</c> function.</summary>
internal static class CommandStrategy
{
    /// <summary>Prints usage and returns a non-zero exit code (no/unknown verb).</summary>
    public static Func<ConfigState, int> EmptyArgs => (state) =>
    {
        state.Error.WriteLine("usage: agora <init|run|resume|ingest|validate|eval> [options]");
        state.Error.WriteLine("  init     [--output <file>]   guided config builder");
        state.Error.WriteLine("  run      --config <file> --input <text> [--agent <id>] [--graph] [--stream] [--checkpoint <dir>] [--run-id <id>]");
        state.Error.WriteLine("  resume   --config <file> --checkpoint <dir> --run-id <id>");
        state.Error.WriteLine("  eval     --config <file> --scenario <file.json>");
        return 1;
    };

    /// <summary>Runs the guided config wizard, writing to <c>--output</c> (or the default path).</summary>
    public static Func<ConfigState, int> Init => (state) =>
    {
        return ConfigWizard.Run(state.In, state.Out, state.Error,
                state.Options.TryGetValue("output", out var path) ? path : null);
    };

    /// <summary>Runs an eval scenario (<c>--scenario</c>) against a config and reports PASS/FAIL.</summary>
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
            state.Out.WriteLine("PASS");
            return 0;
        }
        state.Error.WriteLine("FAIL");
        foreach (var failure in result.Failures)
            state.Error.WriteLine($"  - {failure}");
        return 1;
    };

    /// <summary>Loads and validates a config, printing OK on success or INVALID on failure.</summary>
    public static Func<ConfigState, int> Validate => (state) =>
    {
        try
        {
            ConfigLoader.Load(Require(state.Options, "config"));
        }
        catch (ConfigException e)
        {
            state.Error.WriteLine($"INVALID: {e.Message}");
            return 1;
        }
        state.Out.WriteLine("OK");
        return 0;
    };

    /// <summary>Runs a single agent or the graph, with optional streaming and checkpointing.</summary>
    public static Func<ConfigState, int> Run => (state) =>
    {
        var isGraph = state.Options.ContainsKey("graph");
        var checkpoints = state.Options.TryGetValue("checkpoint", out var cpDir)
            ? new FileCheckpointStore(cpDir) : null;
        var stream = state.Options.ContainsKey("stream");
        Action<string>? onChunk = stream ? chunk => state.Out.Write(chunk) : null;
        var runtime = Runtime.FromConfig(Require(state.Options, "config"),
            state.Provider ?? throw new InvalidOperationException("no chat provider supplied"),
            backend: state.Backend, approvalHandler: state.ApprovalHandler,
            conflictResolver: state.ConflictResolver, checkpointStore: checkpoints);
        if (isGraph)
        {
            var result = runtime.RunAsync(Require(state.Options, "input"), state.Options.GetValueOrDefault("run-id"), onChunk)
                .GetAwaiter().GetResult();
            if (stream) state.Out.WriteLine(); else state.Out.WriteLine(result.Output);
            if (checkpoints is not null)
                state.Error.WriteLine($"run-id: {result.RunId}");
        }
        else
        {
            var result = runtime.RunAgentAsync(Require(state.Options, "agent"), Require(state.Options, "input"), onChunk)
                .GetAwaiter().GetResult();
            if (stream) state.Out.WriteLine(); else state.Out.WriteLine(result.Output);
        }
        return 0;
    };

    /// <summary>Resumes a previously checkpointed graph run by <c>--run-id</c>.</summary>
    public static Func<ConfigState, int> Resume = (state) =>
    {
        var runtime = Runtime.FromConfig(Require(state.Options, "config"),
            state.Provider ?? throw new InvalidOperationException("no chat provider supplied"),
            backend: state.Backend, approvalHandler: state.ApprovalHandler,
            conflictResolver: state.ConflictResolver,
            checkpointStore: new FileCheckpointStore(Require(state.Options, "checkpoint")));
        var result = runtime.ResumeAsync(Require(state.Options, "run-id")).GetAwaiter().GetResult();
        state.Out.WriteLine(result.Output);
        return 0;
    };

    /// <summary>Ingests the config's RAG sources into its vector store and reports the chunk count.</summary>
    public static Func<ConfigState, int> Ingest => (state) =>
    {
        var runtime = Runtime.FromConfig(Require(state.Options, "config"),
            state.Provider ?? throw new InvalidOperationException("no chat provider supplied"),
            backend: state.Backend);
        if (runtime.Rag is null)
        {
            state.Error.WriteLine("ERROR: config has no enabled 'rag' section");
            return 1;
        }
        var ingestCfg = runtime.Config.Rag?.Ingest;
        var count = runtime.Retrieval!.IngestAsync(
                ingestCfg?.Sources ?? new List<string>(),
                chunkSize: ingestCfg?.ChunkSize ?? 800, overlap: ingestCfg?.ChunkOverlap ?? 120)
            .GetAwaiter().GetResult();
        state.Out.WriteLine($"ingested {count} chunks");
        return 0;
    };

    /// <summary>Runs a command, converting any thrown exception into an error message and exit code 1.</summary>
    public static int Execute(Func<ConfigState, int> command, ConfigState state)
    {
        try
        {
            return command(state);
        }
        catch(Exception ex)
        {
            state.Error.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }
    }

    /// <summary>Returns a required option's value, or throws if it's missing.</summary>
    private static string Require(Dictionary<string, string> options, string name)
        => options.TryGetValue(name, out var value)
            ? value
            : throw new ConfigException($"missing required option --{name}");
}