using System.Globalization;
using System.Text.Json;
using Agora.Configuration;
using Agora.Eval;
using Agora.Eval.Quality;
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
        state.Error.WriteLine("usage: agora <init|run|resume|ingest|validate|eval|eval-quality> [options]");
        state.Error.WriteLine("  init         [--output <file>]   guided config builder");
        state.Error.WriteLine("  run          --config <file> --input <text> [--agent <id>] [--graph] [--stream] [--checkpoint <dir>] [--run-id <id>]");
        state.Error.WriteLine("  resume       --config <file> --checkpoint <dir> --run-id <id>");
        state.Error.WriteLine("  serve-mcp    --config <file>   local read-only MCP stdio server exposing rag_search");
        state.Error.WriteLine("  purge-kb-log --config <file> [--before <ISO-8601 date>]   purge the KB mutation audit log");
        state.Error.WriteLine("  eval         --config <file> --scenario <file.json>");
        state.Error.WriteLine("  eval-quality --suite <dir|file> --judge-config <file> [--out <report.json>]   (live; set AGORA_EVAL_LIVE=1)");
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
        var scenarioPath = state.Require("scenario");
        if (!File.Exists(scenarioPath))
            throw new ConfigException($"scenario file not found: {scenarioPath}");
        var scenario = JsonSerializer.Deserialize<Scenario>(
            File.ReadAllText(scenarioPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new ConfigException("scenario file is empty or invalid");
        var result = ScenarioRunner.RunAsync(state.Require("config"), scenario)
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

    /// <summary>Runs the LLM-judge quality suite (<c>--suite</c>) against a real provider and a judge model
    /// (<c>--judge-config</c>), printing per-scenario scores and optionally a JSON report (<c>--out</c>).
    /// Opt-in: refuses unless <c>AGORA_EVAL_LIVE</c> is set, so CI stays offline by default.</summary>
    public static Func<ConfigState, int> EvalQuality => (state) =>
    {
        if (Environment.GetEnvironmentVariable("AGORA_EVAL_LIVE") is not ("1" or "true"))
        {
            state.Error.WriteLine("eval-quality runs real models; set AGORA_EVAL_LIVE=1 to enable it.");
            return 2;
        }
        var sut = state.RequireProvider();
        var scenarios = QualitySuiteLoader.Load(state.Require("suite"));
        var judge = QualityJudgeFactory.FromConfig(state.Require("judge-config"), sut);

        var suite = QualityRunner.RunSuiteAsync(scenarios, sut, judge, state.Backend).GetAwaiter().GetResult();
        state.Out.WriteLine(QualityReport.Human(suite));
        if (state.Options.TryGetValue("out", out var outPath))
        {
            File.WriteAllText(outPath, QualityReport.Json(suite));
            state.Error.WriteLine($"report: {outPath}");
        }
        return suite.ErrorCases > 0 ? 1 : 0;
    };

    /// <summary>Loads and validates a config, printing OK on success or INVALID on failure.</summary>
    public static Func<ConfigState, int> Validate => (state) =>
    {
        try
        {
            var config = ConfigLoader.Load(state.Require("config"));
            // Fail loud: a writable KB (rag_write) needs a real embedder and a resolvable judge model.
            RagWriteValidator.Validate(config);
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
        var runtime = state.BuildRuntime(checkpoints);
        state.RequireInteractiveForRagWrite(runtime.Config);
        var result = isGraph
            ? runtime.RunAsync(state.Require("input"), state.Options.GetValueOrDefault("run-id"), onChunk)
                .GetAwaiter().GetResult()
            : runtime.RunAgentAsync(state.Require("agent"), state.Require("input"), onChunk)
                .GetAwaiter().GetResult();
        if (stream) state.Out.WriteLine(); else state.Out.WriteLine(result.Output);
        if (isGraph && checkpoints is not null)
            state.Error.WriteLine($"run-id: {result.RunId}");
        if (result.Metrics is { } metrics)
            state.Error.WriteLine(metrics.ToSummary());
        return 0;
    };

    /// <summary>Resumes a previously checkpointed graph run by <c>--run-id</c>.</summary>
    public static Func<ConfigState, int> Resume = (state) =>
    {
        var runtime = state.BuildRuntime(new FileCheckpointStore(state.Require("checkpoint")));
        state.RequireInteractiveForRagWrite(runtime.Config);
        var result = runtime.ResumeAsync(state.Require("run-id")).GetAwaiter().GetResult();
        state.Out.WriteLine(result.Output);
        if (result.Metrics is { } metrics)
            state.Error.WriteLine(metrics.ToSummary());
        return 0;
    };

    /// <summary>Runs a local, read-only MCP stdio server exposing only <c>rag_search</c> over the config's
    /// knowledge base (the same read pipeline the CLI uses). No write tool is exposed and no network port is
    /// opened; the server runs until the client closes the stdio transport.</summary>
    public static Func<ConfigState, int> ServeMcp => (state) =>
    {
        var runtime = state.BuildRuntime();
        if (runtime.Rag is null)
        {
            state.Error.WriteLine("ERROR: config has no enabled 'rag' section to serve");
            return 1;
        }
        var server = state.RequireMcpServer();
        server.ServeAsync(async (query, ct) => (await runtime.Rag.RunAsync(query, ct)).AsContext())
            .GetAwaiter().GetResult();
        return 0;
    };

    /// <summary>Purges the KB mutation audit log (next to the config): all records, or those older than
    /// <c>--before</c> (ISO-8601). The log retains deleted KB content, so this is its retention/erasure path.</summary>
    public static Func<ConfigState, int> PurgeKbLog => (state) =>
    {
        var configDir = Path.GetDirectoryName(Path.GetFullPath(state.Require("config"))) ?? ".";
        var log = new FileKbMutationLog(FileKbMutationLog.DefaultPath(configDir));
        Func<KbMutation, bool> remove = state.Options.TryGetValue("before", out var raw)
            ? m => m.Timestamp < DateTimeOffset.Parse(
                raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
            : _ => true;
        var removed = log.PurgeAsync(remove).GetAwaiter().GetResult();
        state.Out.WriteLine($"purged {removed} KB mutation log record(s)");
        return 0;
    };

    /// <summary>Ingests the config's RAG sources into its vector store and reports the chunk count.</summary>
    public static Func<ConfigState, int> Ingest => (state) =>
    {
        var runtime = state.BuildRuntime();
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
}