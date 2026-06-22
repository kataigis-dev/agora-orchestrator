using Agora.Configuration;
using Agora.Runs.Contracts;
using Agora.Runs.Models;
using Agora.Runs.Concretes;

namespace Agora.Api;

/// <summary>Maps the run lifecycle HTTP endpoints (start, status, approvals, ingest).</summary>
public static class RunEndpoints
{
    /// <summary>Registers <c>POST /runs</c>, <c>GET /runs/{id}</c>, <c>POST /runs/{id}/approvals</c>,
    /// and <c>POST /ingest</c> on the app.</summary>
    public static void MapAgoraRunEndpoints(this WebApplication app)
    {
        app.MapPost("/runs", async (StartRunRequest req, AgoraConfig cfg, IRunStore store, RunQueue queue) =>
        {
            if (req.Mode == "agent")
            {
                if (string.IsNullOrWhiteSpace(req.Agent) || !cfg.Agents.ContainsKey(req.Agent))
                    return Results.BadRequest(new { error = $"unknown agent '{req.Agent}'" });
            }
            else if (req.Mode == "graph")
            {
                if (cfg.Graph is null)
                    return Results.BadRequest(new { error = "config has no graph" });
            }
            else
            {
                return Results.BadRequest(new { error = $"unknown mode '{req.Mode}'" });
            }

            var rec = store.Create(req.Mode, req.Agent, req.Input);
            await queue.EnqueueAsync(rec.Id);
            return Results.Accepted($"/runs/{rec.Id}", new StartRunResponse(rec.Id));
        });

        app.MapGet("/runs/{id}", (string id, IRunStore store, ApprovalGate gate) =>
        {
            var rec = store.Get(id);
            if (rec is null)
                return Results.NotFound(new { error = $"unknown run '{id}'" });
            var pending = gate.Pending(id)
                .Select(p => new PendingApprovalDto(p.Id, p.AgentId, p.FunctionName, p.Arguments))
                .ToList();
            return Results.Ok(new RunStatusResponse(
                rec.Id, rec.Status.ToString(), rec.Mode, rec.AgentId, rec.Output, rec.Error, pending));
        });

        app.MapPost("/runs/{id}/approvals", (string id, ApprovalsRequest body, IRunStore store, ApprovalGate gate) =>
        {
            if (store.Get(id) is null)
                return Results.NotFound(new { error = $"unknown run '{id}'" });
            var resolved = body.Approvals.Count(d => gate.Resolve(d.Id, d.Approved));
            if (resolved == 0)
                return Results.NotFound(new { error = "no matching pending approvals" });
            return Results.Ok(new { resolved });
        });

        app.MapPost("/ingest", async (AgoraRuntimeFactory runtimes) =>
        {
            var retrieval = runtimes.Retrieval;
            if (retrieval?.Pipeline is null)
                return Results.BadRequest(new { error = "config has no enabled 'rag' section" });
            var ingestCfg = runtimes.Config.Rag?.Ingest;
            var count = await retrieval.IngestAsync(
                ingestCfg?.Sources ?? new List<string>(),
                chunkSize: ingestCfg?.ChunkSize ?? 800, overlap: ingestCfg?.ChunkOverlap ?? 120);
            return Results.Ok(new IngestResponse(count));
        });
    }
}
