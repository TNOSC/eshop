# Rule — domain events and the outbox

## `[DomainEventName]` is a wire contract

```csharp
[DomainEventName("catalog.product-created.v1")]
public sealed record ProductCreatedDomainEvent(…) : IDomainEvent;
```

That string is persisted into outbox rows and is how `DomainEventTypeRegistry` resolves a row back to
a CLR type when the processor delivers it.

**Once shipped, the name is immutable.** Renaming it strands every unprocessed row already in the
outbox — the registry cannot resolve the old name, and delivery fails permanently for those rows.

- Format: `<context>.<event-in-kebab-case>.v<N>`.
- Changing the event's **shape** in a breaking way ⇒ new type, `v2`, keep `v1` handled until the
  backlog drains.
- Renaming the CLR type is free; changing the attribute value is not.
- Names must be unique solution-wide — `FrameworkInvariantTests.DomainEventNames_Should_Be_Unique`
  enforces it.

## Events carry primitives

Ids and flat values, never entities, value objects or typed ids. The payload is serialized and read
back later, possibly by code that has moved on:

```csharp
[DomainEventName("catalog.product-price-changed.v1")]
public sealed record ProductPriceChangedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid ProductId,
    decimal OldAmount,
    string OldCurrency,
    decimal NewAmount,
    string NewCurrency) : IDomainEvent;
```

Every event carries its own `Id` (`Guid.CreateVersion7()`) and `OccurredOnUtc`, set by the aggregate
when it raises the event.

## The aggregate raises, the handler does not

`AddDomainEvent(...)` is called inside the aggregate's factory or transition method — the same method
that mutates state and calls `IncrementVersion()`. A handler that constructs and raises a domain
event has taken a decision that belongs to the domain.

`UnitOfWork.SaveChangesAsync` converts raised events into outbox rows **inside the aggregate's own
transaction**, which is what makes the write and the event atomic. This is also why all contexts share
one database and one `DbContext` pair.

## Delivery is at-least-once — handlers must be idempotent

`OutboxProcessor` claims rows with `FOR UPDATE SKIP LOCKED` and marks them processed after the
handler returns. A crash between the two redelivers the event. So a domain event handler must be safe
to run twice: check-then-act on its own state, upsert rather than insert, and never assume "first
time".

**Or mark it `[Idempotent]`** and let the inbox do it — `IdempotencyDecorator` claims
`IDomainEvent.Id` for that handler in the same transaction as the handler's own writes, so a
redelivery is skipped rather than applied twice. See `.claude/rules/idempotency.md`. Hand-written
idempotency is still fine where it is natural; the attribute is for handlers where it is not.

The claim also takes a **lease** (`next_attempt_on_utc` pushed out by `BaseBackoff`). `SKIP LOCKED`
only holds the row until the claim statement commits, and processing happens after that — without
the lease a second processor polling mid-batch would re-claim the same still-unprocessed rows.
Do not remove it, and keep `BaseBackoff` comfortably longer than a batch takes to process.

A handler that throws leaves the row unprocessed for retry — do not swallow exceptions to make a
poison message disappear; that silently drops the event.

## Two retries, and they are not the same retry

| | Where | Trigger | Delay | Survives a restart |
|---|---|---|---|---|
| `[Retry(n)]` | in-process, around the handler | retriable `BaseException` only | `200ms · 2^(attempt-1)` | no |
| Outbox | the row itself | any exception reaching `OutboxProcessor` | `BaseBackoff · 2^(attempts-1)` | yes |

`[Retry(n)]` is the **fast** retry for a blip that clears in milliseconds; the outbox is the durable
one and remains the actual guarantee. They compose — an exhausted in-process retry still propagates,
fails the row, and hands over. Three rules:

- **It is opt-in on this pipeline.** No `[Retry]` means one attempt, unlike commands and queries
  where three is the default. The outbox is already retrying; a second invisible layer on every
  handler is not a default worth having.
- **Keep `n` small — 6 or less at the default `BaseBackoff`.** Total in-process delay is
  `200ms · (2^(n-1) − 1)`, which passes the 10s claim lease at `n = 7`. A handler that retries past
  its lease lets another processor claim the same message while it is still working.
- **Pair it with `[Idempotent]` when the handler writes anything.** Retrying a handler that already
  committed part of its work on attempt 1 re-applies it on attempt 2. Nothing enforces the pairing.

Note that today nothing in this solution throws a retriable `BaseException` from the persistence
path — a Postgres deadlock arrives as `NpgsqlException`, which does not qualify. So `[Retry]` here is
correct but inert until those are wrapped into `TransientFailureException`.

## One event, many handlers — they are isolated

`DomainEventsPublisher` runs each handler in its own `try`/`catch` and returns a
`DomainEventDeliveryReport` naming the ones that threw. A failing handler **never** stops the
handlers after it: one context's broken subscriber is not a reason to withhold the event from
another's.

Two consequences worth holding on to:

- **Successful siblings only stay successful if they are `[Idempotent]`.** A broken handler forces
  the message to be redelivered, and on redelivery a non-idempotent sibling runs again. The attribute
  is what makes its inbox claim skip it.
- **Anything memoised per handler must be keyed on the unwrapped handler type**, never on the closed
  decorator type. Sibling handlers of one event share
  `IdempotencyDecorator.DomainEventHandler<TEvent>`, so keying on that hands every sibling the first
  one's name and attributes — they end up sharing a single inbox claim and only one ever runs.
  `HandlerChain.Unwrap` therefore does not cache; its callers key their own caches on what it returns.

## Dead letters

When a message runs out of outbox attempts it is **moved** to `outbox.dead_letters` and deleted from
`outbox_messages` — one row per **(event, handler)** — in the same transaction, so it can never be
both queued and dead-lettered. A failure that never reached a handler (unresolvable contract name,
unreadable payload) lands there too, with a null `handler`.

```csharp
IDeadLetterQueue queue;                      // scoped, registered by AddPersistence
await queue.ListAsync(skip: 0, take: 50, cancellationToken: ct);   // pending rows, newest first
await queue.ReplayAsync(deadLetterId: id, cancellationToken: ct);  // re-invokes ONLY that handler
await queue.DiscardAsync(deadLetterId: id, cancellationToken: ct);
```

- **Replay is handler-scoped, never a re-publish.** The siblings usually succeeded already, and
  re-publishing would re-run any of them that is not `[Idempotent]`. The failed handler's own claim
  rolled back with its work, so nothing blocks it from being retried.
- A replay that fails again is an outcome, not an exception: `replay_count` goes up, the newer error
  is recorded, and the row stays.
- A recovered row is **kept**, stamped `replayed_on_utc`, and drops out of listings. The queue is an
  audit trail as much as a work list.
- **There is no HTTP surface.** Nothing wires authentication yet, and replay and discard are not
  operations to leave reachable. Endpoints are a small follow-up once Identity exists.

## Cross-context integration

Context B reacts to context A's event in **B's** `EventHandlers/` folder, against B's own types. B
never references A's aggregate, and A never calls into B. This is the only sanctioned coupling
between bounded contexts.

## Checklist

- [ ] `[DomainEventName]` present, `<context>.<name>.v<N>`, unique, and not a rename of a shipped one.
- [ ] Payload is flat primitives and ids only.
- [ ] `Id` and `OccurredOnUtc` populated by the aggregate.
- [ ] Raised inside the aggregate method that mutates state, alongside `IncrementVersion()`.
- [ ] Any handler for it is idempotent.
