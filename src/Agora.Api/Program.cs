using Agora.Api;
using Agora.AgentFramework;
using Agora.Agents;
using Agora.Configuration;
using Agora.Providers;
using Agora.Rag;
using Agora.Runs;

var builder = WebApplication.CreateBuilder(args);

// Resolve the config path: --config <path> arg or AGORA_CONFIG env var.
var configPath = GetConfigPath(args) ?? Environment.GetEnvironmentVariable("AGORA_CONFIG");
var configDir = configPath is null ? null : Path.GetDirectoryName(Path.GetFullPath(configPath));

// Shared singletons. Tests override IChatProvider / IToolAgentFactory / AgoraConfig.
builder.Services.AddSingleton<IChatProvider>(_ => new AgentFrameworkChatProvider());
builder.Services.AddSingleton<IToolAgentFactory>(_ => new AgentFrameworkToolAgentFactory());
builder.Services.AddSingleton(_ =>
    configPath is null ? new AgoraConfig() : ConfigLoader.Load(Path.GetFullPath(configPath)));
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<AgoraConfig>();
    var provider = sp.GetRequiredService<IChatProvider>();
    var rag = RagFactory.Build(cfg, provider);
    return new AgoraRuntimeFactory(cfg, configDir, provider, sp.GetRequiredService<IToolAgentFactory>(), rag);
});
builder.Services.AddSingleton<IRunStore, InMemoryRunStore>();
builder.Services.AddSingleton<ApprovalGate>();
builder.Services.AddSingleton<RunQueue>();
builder.Services.AddHostedService<RunExecutor>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/agents", (AgoraConfig cfg) =>
    Results.Ok(cfg.Agents.Select(kv => new AgentInfo(
        kv.Key, kv.Value.Role, kv.Value.Tools, kv.Value.Approvals)).ToList()));

app.MapAgoraRunEndpoints();

app.Run();

// Extracts the value of the --config argument, or null when absent.
static string? GetConfigPath(string[] args)
{
    var i = Array.IndexOf(args, "--config");
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

/// <summary>Entry-point marker class made public so integration tests can host the API.</summary>
public partial class Program { }
