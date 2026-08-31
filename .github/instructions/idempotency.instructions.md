---
description: "[Idempotent] claims its key in the handler own transaction; ambient Idempotency-Key, replay semantics, and the two constraints behind it"
applyTo: "src/server/**/*.cs,lib/**/*.cs"
---

# Rule — `[Idempotent]` and the one-transaction claim

**A handler marked `[Idempotent]` claims its key in the same transaction as its own writes.**
Never in a separate transaction, never before the handler runs and committed independently, never
"record it afterwards".

`IdempotencyDecorator` is registered **innermost** in the command pipeline — inside
`TransactionDecorator`, directly around the handler — and is the only decorator on the domain-event
pipeline:

```
Commands:  Logging → Exception → Validation → Retry → CacheInvalidation → Transaction → Idempotency → Handler
Events:    Retry → Idempotency → Handler
```

`Retry` is outside `Idempotency` on both pipelines, and must stay there: each attempt needs its own
transaction, and a retry *inside* the claim would run on a transaction Postgres has already aborted
(`25P02`). From outside, a failed attempt discards the claim along with the handler's partial work,
so the next attempt re-claims and re-runs cleanly — a retried `[Idempotent]` handler still produces
exactly one effect.

## Why

Any two-phase variant has a window that loses data, and the failure is silent:

| Variant | The window |
|---|---|
| Claim commits, then handler runs | Crash in between burns the key with no effect. The client's retry is answered "already done" for work that never happened. |
| Handler commits, then claim is recorded | Crash in between leaves an effect with no key. The retry runs it a second time — the exact duplicate the attribute exists to prevent. |
| Claim and handler in separate transactions with a status column | Both windows, plus a reconciliation job to decide what an abandoned `InProgress` row means. |

One transaction has no window: claim and effect commit together or roll back together.

**This is also why the table needs no status column.** Under READ COMMITTED, a concurrent duplicate's
`INSERT … ON CONFLICT` *blocks* on the first transaction's uncommitted row rather than seeing it. It
resumes only once the first has settled — replaying if it committed, taking the key if it rolled
back. An "in progress" state is never observable, so it never needs representing. Adding a status
column would be inventing a state the database already serialises for you.

## How

```csharp
[Idempotent]
[CacheTag(CacheTags.Catalog)]
internal sealed class CreateProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCommand, ProductId>

[Idempotent]
internal sealed class OrderPlacedDomainEventHandler(…) : IDomainEventHandler<OrderPlacedDomainEvent>
```

That is the whole opt-in. The handler stays unaware — no key parameter, no dedupe branch (which
`NoBusinessBranchingTests` would reject anyway), no store injected.

| | Command | Domain event |
|---|---|---|
| Key | Caller's `Idempotency-Key`, read from `IdempotencyKeyContext` | `IDomainEvent.Id` |
| Scoped by | Handler's full type name | Handler's full type name |
| Table | `idempotency.requests` | `outbox.processed_events` |
| Duplicate | Replays the recorded response | Skips the handler |
| Missing key | `Idempotency.KeyMissing` → 400 | n/a |

- **The key is ambient, never a parameter.** `RequestContextMiddleware` lifts the header into
  `IdempotencyKeyContext`, exactly as it does `Correlation-Id` into `CorrelationIdContext`. No
  endpoint reads headers, and no command carries the key — it is not part of the domain's vocabulary.
  A job or test supplies one by setting `IdempotencyKeyContext.Current` itself.
- **A missing key fails; it does not degrade.** The attribute is opt-in, so its author asked for the
  guarantee. Running unguarded would make it a lie precisely when a retrying client depends on it.
- **A failed command releases its key.** An error `Result` or an exception rolls the transaction back
  and the claim with it, so the caller may retry the same key. Only success burns it.
- **The payload is hashed.** Reusing a key with different content returns `Idempotency.KeyReuse`
  → 409 rather than answering a question the caller did not ask.
- **`[Idempotent]` on a command handler makes `Idempotency-Key` part of that endpoint's contract.**
  Say so in the endpoint's `.WithDescription(...)` and in the commit message.

## The two constraints this rests on

Both were discovered the hard way; do not undo either.

1. **The write context must not use a retrying execution strategy.** EF Core refuses a user-initiated
   transaction while one is configured. `AddNpgsqlDbContext<EShopWriteDbContext>` therefore passes
   `settings.DisableRetry = true`. Retry policy is not lost — it is owned explicitly by
   `RetryDecorator` and `[Retry(n)]`, which can see the `Result`. The read context keeps the strategy;
   it never opens a transaction.
2. **Scopes holding an `IUnitOfWork` must be disposed asynchronously.** `UnitOfWork<TContext>`
   implements only `IAsyncDisposable`, and `IServiceScope.Dispose()` throws for such a service. Any
   code creating a scope a handler will run in uses `CreateAsyncScope()` — `DomainEventsPublisher`
   does. Getting this wrong surfaces as an event failing *after* its handler already succeeded.

## Retention

`IdempotencyOptions` (`Retention` 24h, `CleanupInterval` 1h, `BatchSize` 500) drives
`IdempotencyCleanupBackgroundService`. Retention bounds table size; it does **not** license reusing a
key — an expired but uncollected row still blocks its key, which is the safe direction to fail in.

## Checklist

- [ ] `[Idempotent]` is on the **handler**, not the command or the event.
- [ ] The handler contains no dedupe branch of its own.
- [ ] `IdempotencyDecorator` is registered before `TransactionDecorator` in `AddCommands`, so it ends
      up innermost — the last `TryDecorate` call becomes outermost.
- [ ] A new command response type round-trips through `IdempotencySerialization.Options`; a
      strongly-typed id needs no work, `EntityIdJsonConverterFactory` already covers it.
- [ ] An endpoint whose handler is `[Idempotent]` documents the header.
- [ ] There is an integration test proving the effect happens **once** — a unit test asserting the
      attribute is present proves only that someone typed it.

## Test coverage

`CreateProductIdempotencyTests` covers the command half against a real database: retry replays,
key reuse conflicts, a missing key writes nothing, a failed command frees its key, and concurrent
duplicates create exactly one product. `InboxIdempotencyTests` covers the event half by resetting a
processed outbox row back to pending — the exact state a crash between publish and mark leaves —
and asserting no second delivery. A new context's first `[Idempotent]` handler deserves the same.
