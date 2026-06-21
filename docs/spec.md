# Structured specs (spec-driven development)

Agora can treat the specification as a **structured, machine-checkable artifact** instead of free
text buried in agent prompts or fuzzy RAG entries. Requirements get stable ids and acceptance
criteria; implementation tasks trace back to the requirements they satisfy; a validator enforces the
invariants on every write. This is the foundation for turning gate decisions from "the model says
done" into deterministic checks.

> **Status.** Phases 1–3 are implemented: the structured schema, a persistent store (file or
> RAG-over-MCP), the typed `spec_*` tools and write-time validation (phase 1); real build/test
> execution bound to acceptance criteria with deterministic verification (phase 2 — see
> [Verifying against reality](#verifying-against-reality)); and the traceability completion gate that
> consumes requirement↔task↔check coverage, surfaced both as the `spec_gate` tool and on run metrics
> (phase 3 — see [Gating completion](#gating-completion)).

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
| `spec_gate` | `()` | Read-only completion gate: the traceability matrix + a `COMPLETE`/`INCOMPLETE` verdict (see [Gating completion](#gating-completion)) |
| `spec_propose_requirement` | `(title, description, priority, acceptance)` | Add a `Proposed` requirement; `acceptance` is one criterion per line; returns the new id |
| `spec_bind_check` | `(requirementId, criterionId, kind, expression)` | Bind a criterion to a check so it can be machine-verified (see phase 2) |
| `spec_set_status` | `(requirementId, status)` | Advance a requirement's lifecycle status |
| `spec_add_task` | `(description, kind, requirementIds)` | Add a task linking comma/space-separated requirement ids (must exist); returns the new id |
| `spec_link_task` | `(taskId, requirementIds)` | Link an existing task to more requirements |

## Validation rules (`SpecValidator`)

Errors block a write; warnings surface gaps without blocking. The whole-spec completion gate that
consumes coverage is [`TraceabilityValidator`](#gating-completion) (phase 3).

| Code | Severity | Rule |
|---|---|---|
| `duplicate-requirement-id` / `duplicate-task-id` / `duplicate-criterion-id` | error | Ids are unique |
| `missing-acceptance-criteria` | error if `require_criteria`, else warning | Every requirement has ≥1 criterion |
| `task-without-requirement` | error | Every task references at least one requirement |
| `dangling-requirement-ref` | error | Referenced requirement ids exist |
| `empty-check-expression` | warning | Non-`Manual` checks carry an expression |
| `uncovered-requirement` | warning | An approved requirement has an implementing task |

## Verifying against reality

Phase 2 makes acceptance criteria executable, so a gate asserts what the build/test runner reports
rather than what the model claims.

**Checks** are a fixed allow-list of named commands under the `checks` section. They run as real
processes with **no shell** — the executable and each argument are passed directly, so a value can
never be interpreted as shell syntax — and an agent can only invoke them *by name*, never as a raw
command line. `{key}` placeholders in a check's arguments are substituted token-by-token at call time.

```yaml
checks:
  workdir: .                 # relative to the config dir
  timeout: 300               # seconds, per check
  commands:
    build: { command: dotnet, args: [build] }
    test:  { command: dotnet, args: [test, --filter, "{filter}"] }
```

**Binding.** `spec_bind_check` attaches a `SpecCheck` to a criterion: `Kind` `Test`/`Command` names a
configured check (with optional `key=value` args, e.g. `test filter=AuthTests`), `FileExists` names a
path, `Manual` stays human-judged.

**Two execution tools** (in `Agora.AgentFramework/CheckTools.cs`, gated by the allow-list and
approval-gateable):

| Tool | Signature | Effect |
|---|---|---|
| `run_check` | `(name, args)` | Run a configured check and return its real result (`args` is `key=value` pairs) |
| `spec_verify` | `(requirementId)` | Run the checks bound to a requirement's criteria; on a deterministic pass mark it `Verified` and record evidence on covering tasks |

`AcceptanceVerifier` (core) derives the verdict purely from exit codes: a requirement is verified only
when it has criteria, every automated one passed, and none still need manual sign-off. `spec_verify`
will not advance a requirement on a narrative claim of success — only on a passing check. `ICheckRunner`
/ `ProcessCheckRunner` live in the core; the runner is built from config (no resolver needed) and
exposed on `Runtime.CheckRunner`.

## Gating completion

Phase 3 turns per-requirement verification into a whole-spec **completion gate**: a run is "done" only
when the committed scope is fully traced and verified, not when the graph happens to reach `END`.

`TraceabilityValidator.Analyze` (core) projects the `SpecDocument` into a `TraceabilityReport` — a
matrix of every requirement with the tasks that implement it and whether its acceptance is
machine-checkable — and derives a deterministic verdict. A requirement is **in scope** once it is
`Approved` or beyond (`Proposed`/`Rejected` are excluded). The gate blocks (an error gap) when:

| Gap | Rule |
|---|---|
| `no-approved-requirements` | nothing has been approved — there is no committed scope to complete |
| `uncovered-requirement` | an in-scope requirement has no implementing task |
| `unverified-requirement` | an in-scope requirement has not reached `Verified` |
| `unsubstantiated-verification` | a requirement is `Verified` but its acceptance is manual/expression-less — a verdict no real check could have produced (i.e. a hand-set status bypassing `spec_verify`) |
| `orphan-task` *(warning)* | a task traces back to no requirement (write-blocked by `SpecValidator`; reported here so the matrix stands alone) |

`report.IsComplete` is true only when there are no error gaps. Two consumers share it:

- **`spec_gate` tool** (`Agora.AgentFramework`, read-only) — returns the matrix + `COMPLETE`/`INCOMPLETE`
  so a QA/gate agent routes `done` only on a real verdict instead of self-assessment. It never mutates
  the spec.
- **`RunMetrics.Traceability`** — when a `spec` store is configured the runtime analyses the persisted
  document at the end of every run and attaches a `TraceabilitySummary` (`Requirements`, `Covered`,
  `Verified`, `Tasks`, `Complete`, plus coverage/verification rates) to `RunResult.Metrics`, so a caller
  or CI can assert completeness programmatically. Reading the store is best-effort — a store failure
  leaves the rest of the metrics intact.

## Example

`examples/agora-spec.yaml` runs the full loop: a `pm` agent records and approves structured
requirements, an `architect` agent adds traceable tasks and binds each acceptance criterion to a
check, and a `qa` agent calls `spec_verify` to mark requirements `Verified` only when their bound
checks actually pass, then `spec_gate` to confirm the whole scope is complete — signalling done only on
a `COMPLETE` verdict and looping back to the architect otherwise.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora-spec.yaml \
  --input "Build a URL shortener with auth" --graph
```

The resulting `spec.json` is the structured, validated, and verified specification, and the run's
`RunResult.Metrics.Traceability` records whether the committed scope was actually covered and verified
— a deterministic completion signal independent of any agent's narrative.
