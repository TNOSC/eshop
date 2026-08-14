# ADR-016: Architecture Rules Mechanised With NetArchTest + Roslyn

## Status

Accepted

## Date

2026-08-14

## Context

This solution encodes a large number of design rules that are easy to state but easy to silently violate
over time as the codebase grows: layer dependency direction, no business branching outside the domain
(ADR-005), no `IConfiguration`/`IOptions<T>` in a consumer constructor (ADR-014), unique
`[DomainEventName]`s (ADR-011), and more. Relying on code review alone to catch every violation does not
scale as contributors and features multiply, and some of these rules (e.g. "no `if` for business logic in
Application") are not naturally expressible as a simple linter rule.

## Decision

Design rules are encoded as executable tests in `Tests.Architecture`, using NetArchTest for dependency-
direction and naming/type-shape rules, and Roslyn (via Mono.Cecil for IL-level inspection, e.g.
`ConfigurationTests`) for rules that need to inspect method bodies or constructor parameters directly —
such as `NoBusinessBranchingTests`'s IL-level branch detection for the "no business `if` in Application/
Infrastructure" rule. A violation fails `dotnet test tests/server/Tnosc.EShop.Server.Tests.Architecture`,
which is part of the definition of done for any feature.

## Rationale

- **IL-level branch detection was chosen over source-level (Roslyn syntax) detection for the
  business-branching rule specifically** because syntax-level `if`-statement scanning is close to 100%
  false positives on async handlers — the compiler-generated state machine reintroduces branching that has
  nothing to do with business logic. Inspecting IL directly (or the compiled method body) lets the rule
  target actual control-flow branching in the *logical* method, filtering out what the `async`/`await`
  transform introduces.
- **A design rule that only lives in a `CLAUDE.md`/rule-file description degrades over time** as the
  codebase grows past what a reviewer can hold in their head on every PR — dependency direction, unique
  event names, and configuration-injection discipline are exactly the kind of rule that's easy to state
  once and then slowly erode without an automated check.
- **"Architecture test failure" is deliberately not the same category as a suppressible analyzer
  warning** (see `analyzer-suppressions.md`) — if one fails, either the code is wrong, or the rule itself
  changed and must be updated deliberately with a stated reason. There is no `#pragma` equivalent for an
  architecture test, and that asymmetry is intentional: these are design decisions, not style preferences.
- Alternative rejected: relying on code review alone — rejected as not scaling with codebase growth, and
  specifically unable to reliably catch IL-shaped violations (like the bug where decorators read
  `innerHandler.GetType()` — since only the innermost decorator ever sees the actual handler type in a
  real chain, every attribute-driven decorator silently became a no-op — a bug that a human reviewer
  plausibly would not have caught by reading the source).

## Consequences

**Easier:**
- A layer-boundary or branching violation is caught at `dotnet test` time, before it reaches review, and
  fails with a specific, actionable assertion message rather than a vague review comment.
- New contributors learn the architecture's hard boundaries from a failing test and its assertion, not
  only from reading prose documentation that may be out of date.

**Harder:**
- Every new design rule this codebase adopts needs its own mechanized test, or it silently reverts to
  "documented but unenforced" — the rule files (`.claude/rules/*.md`) exist partly to record which rules
  already have that enforcement and which do not.
- IL-level tests (`NoBusinessBranchingTests`, `ConfigurationTests`) are more delicate to write and debug
  than source-level ones, and their false-positive edges (e.g. `?.`/`??` operators, compiler-generated
  code) must be understood by whoever maintains them, not just whoever writes new handlers.
