# Structured specs (spec-driven development)

Agora can treat the specification as a **structured, machine-checkable artifact** instead of free
text buried in agent prompts or fuzzy RAG entries. Requirements get stable ids and acceptance
criteria; implementation tasks trace back to the requirements they satisfy; a validator enforces the
invariants on every write. This is the foundation for turning gate decisions from "the model says
done" into deterministic checks.

> **Status.** This is phase 1 of the SDD work: the structured schema, a persistent store (file or
> RAG-over-MCP), the typed `spec_*` tools, and write-time validation. Phase 2 (real build/test
> execution bound to acceptance criteria) and phase 3 (the completion gate consuming
> requirement↔task↔test coverage) build on these types.

## The model

Defined in `src/Agora/Specs/` (framework-free core):

| Type | What it is |
|---|---|
| `Requirement` | Stable id (`R1..Rn`), title, description, MoSCoW `Priority`, lifecycle `Status`, and acceptance criteria |
| `AcceptanceCriterion` | Stable id (`R1.A1`), a statement, and a `SpecCheck` |
| `SpecCheck` | How the criterion is verified: `Kind` (`Manual`/`Test`/`Command`/`FileExists`) + an `Expression` |
| `TaskItem` | Stable id (`T1..Tn`), description, `Kind` lane, the `RequirementIds` it implements, `Status`, and `Evidence` |
| `SpecDocument` | The aggregate (requirements + tasks); immutable, with `WithRequirement`/`WithTask` upserts and `NextRequirementId`/`NextTaskId` |

`RequirementStatus` advances `Proposed → Approved → Implemented → Verified` (or `Rejected`). The
`SpecCheck.Expression` is interpreted per kind — a configured check name for `Test`/`Command`, a
path/glob for `FileExists`, free text for `Manual` — and is what later phases execute.

## Persistence (`ISpecStore`)

The document is the **deterministic source of truth**, kept separate from similarity-based RAG
retrieval. Two stores ship:

- **`file`** (`FileSpecStore`, core) — canonical JSON on disk (default `spec.json` beside the
  config). Durable, diffable, version-controllable. Recommended default.
- **`mcp`** (`McpSpecStore`, `Agora.AgentFramework`) — persists the spec through an MCP server's
  write tool under a stable key and retrieves it via a read/search tool. Use this to keep the spec in
  an external or shared knowledge store. Because retrieval can be semantic, the payload is tagged with
  the key and the JSON object is extracted from whatever surrounding text the tool returns; for strict
  guarantees prefer the file store.

Serialization is shared by both stores via `SpecSerializer` (indented JSON, enums by name), so the
on-disk and over-MCP representations are identical.

## Configuration

```yaml
spec:
  enabled: true
  require_criteria: true        # reject requirements with no acceptance criterion
  store:
    type: file                  # deterministic JSON source of truth
    path: ./spec.json
```

RAG-over-MCP variant:

```yaml
spec:
  enabled: true
  store:
    type: mcp
    server: memory              # an entry under mcp.servers
    write_tool: rag_write       # tool that persists the payload
    read_tool: rag_search       # tool that retrieves it
    write_arg: text             # argument carrying the payload
    query_arg: query            # argument carrying the lookup key
    key: "agora:spec-document"  # stable identifier/query
```

| Field | Default | Meaning |
|---|---|---|
| `enabled` | `true` | Build the spec store and expose `spec_*` tools |
| `require_criteria` | `true` | Enforce ≥1 acceptance criterion per requirement on write |
| `store.type` | `file` | `file` (core JSON) or `mcp` (RAG over an MCP server) |
| `store.path` | `spec.json` | File store path (relative to the config dir) |
| `store.server` | — | `mcp.servers` entry backing the store (mcp only) |
| `store.write_tool` / `read_tool` | `rag_write` / `rag_search` | MCP tools to persist/retrieve (mcp only) |
| `store.write_arg` / `query_arg` | `text` / `query` | Argument names on those tools (mcp only) |
| `store.key` | `agora:spec-document` | Stable key/query identifying the spec (mcp only) |

When `spec` is absent or disabled, Agora behaves exactly as before — the feature is fully opt-in.

## The tools

Agents that allow-list them get typed operations (in `Agora.AgentFramework/SpecTools.cs`). Every
mutation loads the document, applies the change, runs `SpecValidator`, and **only persists if there
are no errors** — otherwise it returns a `rejected (nothing saved): …` message listing the problems.
Like every built-in tool they are gated by the agent's `tools` allow-list and can be `approvals`-gated.

| Tool | Signature | Effect |
|---|---|---|
| `spec_get` | `()` | Render the current requirements (with status + criteria) and tasks |
| `spec_propose_requirement` | `(title, description, priority, acceptance)` | Add a `Proposed` requirement; `acceptance` is one criterion per line; returns the new id |
| `spec_set_status` | `(requirementId, status)` | Advance a requirement's lifecycle status |
| `spec_add_task` | `(description, kind, requirementIds)` | Add a task linking comma/space-separated requirement ids (must exist); returns the new id |
| `spec_link_task` | `(taskId, requirementIds)` | Link an existing task to more requirements |

## Validation rules (`SpecValidator`)

Errors block a write; warnings surface gaps without blocking (the completion gate that consumes them
arrives in phase 3).

| Code | Severity | Rule |
|---|---|---|
| `duplicate-requirement-id` / `duplicate-task-id` / `duplicate-criterion-id` | error | Ids are unique |
| `missing-acceptance-criteria` | error if `require_criteria`, else warning | Every requirement has ≥1 criterion |
| `task-without-requirement` | error | Every task references at least one requirement |
| `dangling-requirement-ref` | error | Referenced requirement ids exist |
| `empty-check-expression` | warning | Non-`Manual` checks carry an expression |
| `uncovered-requirement` | warning | An approved requirement has an implementing task |

## Example

`examples/agora-spec.yaml` runs a two-stage pipeline: a `pm` agent records structured requirements
and approves them, then an `architect` agent breaks each approved requirement into traceable tasks.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora-spec.yaml \
  --input "Build a URL shortener with auth" --graph
```

The resulting `spec.json` is the structured, validated specification — ready for the execution and
traceability phases to verify against.
