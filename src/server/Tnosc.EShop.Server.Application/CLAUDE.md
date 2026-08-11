# Application layer

**Orchestration only.** Load → guard → delegate → persist → return the domain's verdict
unreinterpreted. No reference to Infrastructure or EF Core.

## Layout

```
<Context>/Commands/<Feature>/{CreateProductCommand,CreateProductCommandHandler,CreateProductCommandValidator}.cs
<Context>/Queries/<Feature>/{GetProductByIdQuery,ProductDto}.cs     ← query handlers live in Infrastructure
<Context>/EventHandlers/OrderPlacedDomainEventHandler.cs
```

| Thing | Shape |
|---|---|
| Command | `public sealed record CreateProductCommand(…) : ICommand<ProductId>` |
| Command handler | `internal sealed class …CommandHandler : ICommandHandler<TCommand, TResponse>` |
| Validator | `internal sealed class …CommandValidator : IValidator<TCommand>` |
| Query | `public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto>` |
| DTO | `public sealed record ProductDto(…)` |

## The "no business branching" rule — mechanically enforced

`NoBusinessBranchingTests` (Roslyn) fails the build on any `if`, ternary, `switch` statement or
`switch` expression inside a handler whose condition is not a null test, a `Result`/error state test,
or a cancellation check.

```csharp
if (product is null) return ProductErrors.NotFound(…);        // ✅ existence check
if (price.IsError)   return price.Errors.ToArray();           // ✅ error propagation
foreach (IOrderStep step in _steps) { … }                     // ✅ iterating steps

if (order.Total > 100) discount = 0.1m;                       // ❌ → IDiscountStrategy
if (customer.IsPremium) { … }                                 // ❌ → domain method
if (order.Status == OrderStatus.Draft) { … }                  // ❌ → order.Submit() owns it
var fee = method switch { Card => 1.5m, … };                  // ❌ → strategy + factory
```

## Rules

- **Inject the domain repository contract plus `IUnitOfWork`**; the handler calls
  `SaveChangesAsync` itself. Never inject a `DbContext` — architecture test.
- **Never re-decide the domain's outcome.** Propagate `Errors.ToArray()` as-is; the `ErrorType` and
  code the domain chose are what the endpoint maps to HTTP.
- **Validators do structural checks only** — required raw ids the domain never wraps in a value
  object, DTO shape. Formats, lengths, ranges and uniqueness belong to the value objects, entities
  and factories; re-checking them here lets the two drift apart.
- **No god handlers.** Bloat from cross-cutting concerns → a decorator. Bloat from workflow
  complexity → extract `I<X>Workflow` plus step services (`CustomerCreator`, `OrderInitializer`, …)
  and let the handler delegate to it.
- Handlers return `ValueTask<Result<T>>` and are `internal sealed`.

## Decorator pipeline

Registered in `Extensions/ApplicationExtensions.cs` via `Scrutor.TryDecorate`, innermost first — the
last call becomes outermost. **The order is load-bearing:**

```
Commands:  Logging → Exception → Validation → Retry → CacheInvalidation → Transaction → Idempotency → Handler
Queries:   Logging → Exception → Cacheable → Retry → Handler
Events:    Retry → Idempotency → Handler
```

`Retry` inside `Exception` (it only catches `BaseException`) · `Transaction` inside `Retry` (an
aborted Postgres transaction fails every later statement, `25P02`) · `CacheInvalidation` outside
`Transaction` (invalidating pre-commit lets a reader repopulate from the stale snapshot) ·
`Idempotency` innermost (its claim must commit in the handler's own transaction) ·
`Logging` outermost so exception-mapped failures still log.

On the event pipeline `Retry` sits **outside** `Idempotency` for the same `25P02` reason: the
idempotency decorator opens a transaction per invocation, so a retry inside it would run on a
transaction Postgres has already aborted. From outside, a failed attempt rolls the claim back with
the handler's partial work and the next attempt starts clean.

Opt-in attributes go **on the handler class**: `[Transactional]`, `[Retry(n)]`, `[Idempotent]`,
`[Cacheable(seconds)]`, `[CacheTag(CacheTags.X)]`; `[CacheKey]` goes on query properties.
**`[Idempotent]`** makes the effect happen at most once per key — the caller's `Idempotency-Key` for
a command, `IDomainEvent.Id` for a domain event — and a duplicate command replays the recorded
response. The handler stays unaware: no key parameter, no dedupe branch. See
`.claude/rules/idempotency.md`.
Cache tags are **constants** from `Server.Shared/<Context>/CacheTags.cs`, never string literals —
see `Tnosc.EShop.Server.Shared/CLAUDE.md`.
**`[Transactional]` is the exception, not the rule** — only for a handler spanning several
aggregates/repositories, or one that calls `SaveChangesAsync` more than once.

## Canonical handler

```csharp
[CacheTag(CacheTags.Catalog)]
internal sealed class CreateProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCommand, ProductId>
{
    public async ValueTask<Result<ProductId>> HandleAsync(
        CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        Result<Sku> sku = Sku.Create(value: command.Sku);

        if (sku.IsError)
        {
            return sku.Errors.ToArray();
        }

        Result<Product> product = await ProductFactory.CreateAsync(
            repository: repository, sku: sku.Value, …, cancellationToken: cancellationToken);

        if (product.IsError)
        {
            return product.Errors.ToArray();
        }

        await repository.AddAsync(aggregate: product.Value, cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return product.Value.Id;
    }
}
```

Command handlers are unit-tested with NSubstitute over the repository contract — see `tests/CLAUDE.md`.
