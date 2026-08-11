# lib/ — the reusable framework

Five projects with **no eShop knowledge**. Anything context-specific belongs in `src/server/`.

```
Tnosc.Lib.Domain                      Entity, AggregateRoot, IEntityId/GuidEntityId, ValueObject,
                                      Result/Error/ErrorType, IRepository, IDomainEvent
Tnosc.Lib.Application                 ICommand(Handler), IQuery(Handler), IValidator, IUnitOfWork,
                                      decorators, attributes, exceptions, PagedResult
Tnosc.Lib.Api                         IApiEndpoint, CustomResults, Result → HTTP extensions
Tnosc.Lib.Infrastructure.Persistence  Read/Write DbContext bases, UnitOfWork, RepositoryBase,
                                      outbox, EF conventions, migration hosted service
Tnosc.Lib.Host                        HttpUserContext, GlobalExceptionHandler, RequestContextMiddleware
```

## XML documentation is mandatory here

All five projects set `GenerateDocumentationFile=true`, and warnings are errors ⇒ **`CS1591` fails
the build**. Every public type and member needs `<summary>`, plus `<param>`, `<returns>`,
`<typeparam>` and `<exception>` where they apply. Use `<inheritdoc />` on interface implementations.
Document *why*, not just *what* — the existing files explain the reasoning behind a design, and that
is the house style.

## Rules

- Public API changes here ripple across every bounded context. Prefer additive changes; if you must
  change a signature, update all call sites in the same commit and keep the build clean.
- **`IRepository<TAggregateRoot, TEntityId>` lives in `Lib.Domain`, `IUnitOfWork` in
  `Lib.Application`.** That split is deliberate: enforcing a uniqueness invariant is a domain
  concern (a domain factory must be able to query), while committing a transaction is orchestration.
- Async members return `ValueTask` / `ValueTask<T>`, with `CancellationToken cancellationToken = default` last.
- Decorators are nested types (`LoggingDecorator.CommandHandler<,>`, `.CommandBaseHandler<>`,
  `.QueryHandler<,>`) — `CA1034` is suppressed for exactly this. Every nested handler must implement
  `IHandlerDecorator`, checked by `FrameworkInvariantTests`.
- Handler attributes are read through `HandlerMetadata`, which unwraps the decorator chain. Do not
  read `innerHandler.GetType()` directly — in a real chain only the innermost decorator would see the
  handler and every attribute would silently become a no-op.
- `Result` implements the domain `IResult`; `Error` carries an `ErrorType` and, for
  `ErrorType.Custom`, a `NumericType` that `CustomResults` must honour.
- The outbox is the framework's correctness centrepiece: events serialize through their **concrete**
  type (never the static `IDomainEvent`, which would flatten the payload), each type is resolved by
  its `[DomainEventName]` via `DomainEventTypeRegistry`, and claiming uses `FOR UPDATE SKIP LOCKED`.
- The inbox closes the outbox's at-least-once window: `IdempotencyDecorator` claims the key —
  `Idempotency-Key` for a command, `IDomainEvent.Id` for an event — **in the handler's own
  transaction**, so a key is never burned without its effect. It is registered innermost for that
  reason. Two things it depends on: the write context must not use a retrying execution strategy (EF
  refuses user-initiated transactions under one), and any scope a handler runs in must be created
  with `CreateAsyncScope()` because `IUnitOfWork` is `IAsyncDisposable`-only. See
  `.claude/rules/idempotency.md`.
- `ReadDbContextBase` seals its `SaveChanges`/`SaveChangesAsync` overrides to throw — keep it that way.
- Framework behaviour is covered by `Tests.Unit/Lib*` and `Tests.Integration`; changes here need
  tests in both where they touch persistence.
