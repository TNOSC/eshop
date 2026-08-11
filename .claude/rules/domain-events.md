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

A handler that throws leaves the row unprocessed for retry — do not swallow exceptions to make a
poison message disappear; that silently drops the event.

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
