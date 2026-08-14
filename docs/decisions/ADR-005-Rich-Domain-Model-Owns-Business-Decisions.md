# ADR-005: Rich Domain Model Owns All Business Decisions

## Status

Accepted

## Date

2026-08-14

## Context

CQRS handlers need a place to enforce business rules and invariants (order total must be positive, SKU
uniqueness, valid state transitions). The two common shapes are an anemic domain model, where entities are
plain data bags and handlers/services contain the `if`-branching business logic, or a rich domain model,
where entities, factories and strategies encapsulate the rules themselves.

## Decision

Domain entities are rich: factories decide creation rules, the Strategy pattern handles behavioral
variation, and entities enforce their own invariants. Application handlers contain **no business
branching** — they orchestrate by calling into the domain and propagating its `Result<T>` outcome.
Infrastructure likewise contains no business branching; its only `if`/`try`/`catch` is technical
(translating infrastructure exceptions). This is mechanized, not just documented: `NoBusinessBranchingTests`
in `Tests.Architecture` scans Application and Infrastructure handler methods for disallowed branching.

## Rationale

- **A single source of truth for a rule.** If "is this order total valid" can be decided in a handler,
  a domain method, or a validator, three places can disagree over time as the codebase grows. Putting it
  only in the domain (`Order.Create`, a factory, a strategy) means there is exactly one place to change it
  and exactly one place to unit-test it.
- **Handlers become auditable at a glance.** An Application handler with no business `if` reads as pure
  orchestration — call the domain, persist, return — which is fast to review and fast to trust; the
  interesting logic is concentrated where `NoBusinessBranchingTests` guarantees it must be.
- **Enables the Result-propagation discipline in ADR-002.** "Application must not contain `if` for
  business logic, only propagate domain results" only works if the domain is rich enough to make every
  decision and hand back an already-final outcome.
- Alternative rejected: anemic entities + business logic in handlers/services — this is what "God
  Handlers" (design doc) degrade into: handlers accreting more and more branching and dependencies as
  business logic has nowhere else to live. Decorators and workflow/step services (see ADR-009) solve the
  cross-cutting and orchestration-complexity growth; a rich domain solves where the *decisions* live.

## Consequences

**Easier:**
- Domain unit tests are the primary place business-rule correctness is proven — fast, no I/O, no HTTP.
- Code review can check "does Application branch on business state?" as a bright-line rule rather than a
  judgment call, and a violation fails the build via `NoBusinessBranchingTests` rather than surviving to
  review.

**Harder:**
- Every new business rule requires deciding which domain construct owns it (entity method, factory,
  strategy) rather than just adding an `if` at the call site — more upfront design per rule.
- IL-level branch detection has false-positive edges (async state machines, `?.`/`??` operators); the
  architecture test's tolerance for those has to be understood by anyone adding a handler that trips it.
