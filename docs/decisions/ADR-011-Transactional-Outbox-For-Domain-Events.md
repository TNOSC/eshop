# ADR-011: Transactional Outbox With Immutable Domain-Event Wire Contracts

## Status

Accepted

## Date

2026-08-14

## Context

Aggregates raise domain events when their state changes (e.g. `ProductCreatedDomainEvent`). Other
contexts and background processes need to react to these reliably, without losing an event if the process
crashes between the aggregate write and delivering the event, and without a distributed transaction
spanning the database and a message broker.

## Decision

Domain events are converted into `outbox` schema rows by `UnitOfWork.SaveChangesAsync`, **inside the same
transaction as the aggregate write that raised them** (enabled by ADR-010's single-database design). An
`OutboxProcessor` claims unprocessed rows with `FOR UPDATE SKIP LOCKED` under a lease
(`next_attempt_on_utc`), invokes registered handlers, and marks rows processed after they return — delivery
is at-least-once. Every domain event type carries a `[DomainEventName]` attribute (e.g.
`"catalog.product-created.v1"`) that is persisted into the row and used by `DomainEventTypeRegistry` to
resolve it back to a CLR type on delivery; once shipped, that string is treated as an immutable wire
contract — renaming it strands every unprocessed row already using the old name. A message that exhausts
its retry attempts is moved to `outbox.dead_letters` (one row per event/handler pair) rather than retried
forever or dropped. One event can have many handlers, each running in its own `try`/`catch` so one
handler's failure never withholds the event from another handler.

## Rationale

- **Atomicity without a distributed transaction.** Writing the event row in the same local transaction as
  the aggregate write is what guarantees "the write happened" and "the event was recorded" either both
  commit or both roll back — no two-phase commit, no message-broker transaction needed, because it's the
  same database transaction (this is exactly why ADR-010 keeps all contexts on one database).
- **`[DomainEventName]` as an explicit, versioned string rather than the CLR type name** decouples the
  wire contract from the type name, so the CLR type can be freely renamed without stranding outbox rows —
  only the attribute value is the immutable part, and a breaking payload shape change gets a new `v2` name
  rather than mutating `v1` out from under rows still in flight.
- **Per-handler isolation and per-handler dead-lettering** mean one broken subscriber never blocks
  delivery to a healthy one, and a poison message for one handler doesn't require discarding the event for
  every handler — replay is handler-scoped for the same reason.
- **`FOR UPDATE SKIP LOCKED`** lets multiple processor instances safely compete for rows without blocking
  each other, which single-handedly drove the Postgres choice in ADR-010.
- Alternative rejected: publish directly to a message broker inside the handler — rejected because it
  reintroduces the dual-write problem (the aggregate commit and the publish are not atomic; a crash between
  them either loses the event or double-publishes it) that the outbox pattern exists specifically to avoid.

## Consequences

**Easier:**
- Cross-context reactions (Context B reacting to Context A's event) are reliable by construction — no
  event is lost to a crash between the write and the publish, and delivery survives a process restart.
- A poison event for one handler can be inspected, discarded, or replayed (`IDeadLetterQueue`) without
  affecting delivery to any other handler of the same event.

**Harder:**
- At-least-once delivery pushes the idempotency burden onto every handler — see ADR-012; a handler that
  isn't idempotent and isn't marked `[Idempotent]` will double-apply its effect on redelivery.
- `[DomainEventName]` is a one-way door once shipped: a rename requires a new versioned name and dual
  handling of both names until the backlog drains, not a simple find-and-replace.
