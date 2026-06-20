---
type: concept
title: Signal
tags: [routing, signal, natural-mode, conditional]
related: [edge-types, communication-modes, h2c-protocol, agent-graph, handoff-context]
created: 2026-06-17
updated: 2026-06-20
---

# Signal

Conditional-routing mechanism in **natural** mode. The agent includes special tokens in its output
to communicate state to the orchestrator.

## Syntax

### Signals

```
<<signal name>>
<<signal name=value>>
```

### Artifacts

```
<<artifact key=value>>
```

Examples:
```
I have completed the review. <<signal done>>
Found 3 errors to fix. <<signal fix=3>>
Generated the summary: <<artifact summary=The document describes...>>
```

## Behaviour

- Signals and artifacts are **stripped** from the visible output (`SignalParser.Extract`)
- Signals are recorded in `State.Signals` as `{ "done": true }` or `{ "fix": "3" }`
- Artifacts are recorded in `State.Artifacts` as `{ "summary": "The document describes..." }`
- `GraphExecutor` uses `State.Signals` to resolve `conditional` edges
- `State.ArtifactSummary()` serializes the current artifacts into each subsequent agent's context
- The `handoff` artifact is the handoff payload in handoff mode — see [[handoff-context]]

## Implementation

```csharp
// SignalParser.cs
private static readonly Regex SignalRegex =
    new(@"<<signal\s+([a-zA-Z_]\w*)(?:=([^>]*))?>>", RegexOptions.Compiled);

private static readonly Regex ArtifactRegex =
    new(@"<<artifact\s+([a-zA-Z_]\w*)=([^>]+)>>", RegexOptions.Compiled);

public static (string Output, Dictionary<string, object> Signals, Dictionary<string, string> Artifacts) Extract(string text)
```

## H2C equivalent

In H2C mode the equivalent signal is the block subtype:
```
[STATE:DONE]
```

## Notes

- Signal names are case-sensitive
- An agent can emit multiple signals in the same output
- Only the **last** executed agent's signals matter for routing (`State.Signals` is overwritten
  each step)
