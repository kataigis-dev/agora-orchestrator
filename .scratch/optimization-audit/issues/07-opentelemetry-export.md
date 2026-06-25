# 07 — OpenTelemetry export

Status: ready-for-agent

**Addresses:** F2 (MEDIUM) · **Axes:** observability (supports all three) · **Impact:** ★ · **Effort:** M

## Problem

`src/Agora/Observability/Tracing.cs` is an in-process `SpanRecord` recorder whose own comment says *"OpenTelemetry comes in a later phase."* It is used by tests, not exported. `docs/concepts/10-evaluation-observability.md` and Microsoft Agent Framework both make OTel the standard for latency/token/error-rate telemetry. The seams exist (`IExecutionObserver`, `MetricsExecutionObserver`, `Tracing.BeginSpan`); the exporter does not.

## Proposal

- Back `Tracing` with `System.Diagnostics.ActivitySource` / OTel spans, or add an `IExecutionObserver` that exports run events + token usage as OTel metrics/traces.
- Emit the signals the concepts doc names: latency, token usage, error rate, per-node spans.

## Acceptance criteria

- [ ] Run spans + token/usage metrics are exportable via OpenTelemetry.
- [ ] Existing observers/metrics keep working (composition unchanged).
- [ ] A test asserts spans/metrics are emitted for a graph run.

## Notes

Low reliability impact relative to F1, but it is the production-monitoring counterpart to the eval harness and the cheapest way to make token/error trends visible over time.

## Comments
