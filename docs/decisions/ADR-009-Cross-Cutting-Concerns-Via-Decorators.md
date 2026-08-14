# ADR-009: Cross-Cutting Concerns Via Scrutor Decorators, Not A Mediator Pipeline

## Status

Accepted

## Date

2026-08-14

## Context

Logging, exception translation, validation, retry, cache invalidation, transactions and idempotency all
need to wrap command and domain-event handlers without each handler injecting them directly — injecting
eight or ten cross-cutting dependencies into every handler is exactly the "God Handler" anti-pattern the
design doc calls out. Since there is no mediator pipeline (ADR-001), these concerns need a different
composition mechanism. Hand-rolling open-generic decoration around handler interfaces is non-trivial —
reflection over open generics, correct handling of already-decorated types, and generic variance are easy
to get subtly wrong.

## Decision

Cross-cutting concerns are implemented as decorators around the closed handler interfaces
(`ICommandHandler<,>`, `IQueryHandler<,>`, `IDomainEventHandler<>`), composed at DI registration time with
Scrutor's `TryDecorate`. The registration order is explicit and load-bearing — for commands:
`Logging → Exception → Validation → Retry → CacheInvalidation → Transaction → Idempotency → Handler`; for
domain events: `Retry → Idempotency → Handler`.

## Rationale

- **Solves "God Handlers" without a mediator pipeline.** The rule of thumb — cross-cutting bloat gets
  decorators, workflow bloat gets extracted step services (see the `CustomerWorkflow` example) — needs
  some composition mechanism for the decorator half even without MediatR's `IPipelineBehavior`; Scrutor
  decoration around the same closed handler interfaces used for direct DI injection (ADR-001) fills that
  role.
- **Scrutor over hand-rolled reflection.** `TryDecorate` already solves open-generic decoration correctly;
  reimplementing it is roughly 200 lines of reflection that is "subtly wrong" territory for a solved
  problem — not a place to spend custom code.
- **Explicit order over convention-based pipeline ordering.** Because there is no mediator behavior chain
  to order implicitly by registration or attribute, the decorator order is visible directly in the
  `AddCommands`/`AddQueries`/`AddDomainEventHandlers` registration code, and the last `TryDecorate` call
  becomes outermost — a fact anyone adding a new decorator must know (see `idempotency.md`'s two
  constraints).
- Idempotency specifically must sit **innermost**, directly around the handler and inside the transaction
  decorator, so its database claim commits or rolls back atomically with the handler's own writes — see
  ADR-012. Retry must sit **outside** idempotency on both pipelines so each retry attempt gets its own
  transaction rather than reusing one Postgres has already aborted.

## Consequences

**Easier:**
- A handler stays lean — it injects only its actual business dependencies; every cross-cutting concern is
  opt-in via an attribute (`[Idempotent]`, `[CacheTag]`, `[Retry(n)]`) read by its decorator.
- Adding a new cross-cutting concern for every handler of a kind is one `TryDecorate` registration, not a
  change to every handler.

**Harder:**
- Decorator order is a manually-maintained invariant, not something the type system enforces — getting it
  wrong (e.g. idempotency outside transaction) produces the exact silent-corruption bugs `idempotency.md`
  documents as historical near-misses.
- Attribute-reading decorators must read the handler's own type, not the decorator chain's outer type, or
  every attribute-driven decorator silently becomes a no-op in a real (fully decorated) chain — this was
  bug B2, fixed before any feature slice was built on top of it.
