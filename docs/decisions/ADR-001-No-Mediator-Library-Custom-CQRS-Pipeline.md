# ADR-001: No Mediator Library — Custom CQRS Pipeline

## Status

Accepted

## Date

2026-08-14

## Context

The solution follows CQRS: commands and queries are distinct objects handled by dedicated handlers.
Many .NET Clean Architecture templates reach for MediatR to dispatch these — a single `ISender.Send(...)`
call that resolves the right handler via reflection, with `IPipelineBehavior<TRequest, TResponse>` for
cross-cutting concerns. `Server.Application` and `Server.Api` needed a dispatch mechanism, and cross-cutting
concerns (logging, validation, retry, caching, transactions, idempotency) needed a place to live without
bloating handlers into "god handlers" with eight or ten injected dependencies.

## Decision

Define `ICommandHandler<TCommand, TResponse>` and `IQueryHandler<TQuery, TResponse>` in `lib/`. Minimal-API
endpoints inject the **closed handler interface directly** (e.g. `ICommandHandler<CreateProductCommand,
ProductId>`) and call it — there is no `ISend`/mediator dispatcher, and no third-party mediator package is
referenced anywhere in the solution.

## Rationale

A hand-rolled interface plus direct DI injection was chosen over MediatR because:

- **Reflection-free and startup-validated.** DI resolves the closed generic at container-build time; a
  missing or misregistered handler fails at startup, not on first request.
- **Compiler-checked `TResponse`.** The endpoint's return type is checked by the compiler against the
  handler's contract instead of being erased through a generic `Send<TResponse>(IRequest<TResponse>)` call.
- **Trivially unit-testable.** An endpoint or handler test constructs the dependency directly — no mediator
  mock, no `IRequest` marker interfaces to satisfy.
- **No dispatcher-owned pipeline to fight.** Cross-cutting concerns are composed with Scrutor `TryDecorate`
  around the same closed handler interface (see ADR-009), which keeps them visible in DI registration
  instead of hidden inside a `IPipelineBehavior` chain resolved by convention.
- Depending on a mediator package for what is a direct method call would also conflict with keeping
  `Server.Application` free of third-party framework coupling — see ADR-003 and ADR-004 for the same
  reasoning applied to validation and mapping.

## Consequences

**Easier:**
- Handler resolution failures surface at application startup, not at request time.
- Endpoint and handler unit tests need no mediator infrastructure at all.
- Cross-cutting behavior is visible as an explicit `TryDecorate` registration, not implicit pipeline order.

**Harder:**
- Every new command/query needs its own closed generic DI registration (handled by `Scan()` conventions in
  `AddApplication`), rather than a single `AddMediatR(...)` call picking up all handlers automatically.
- There is no built-in behavior chain — each cross-cutting concern is its own decorator, so the ordering in
  `AddCommands`/`AddQueries` is load-bearing and must be understood by anyone adding one (see
  `idempotency.md`'s pipeline-order table).
