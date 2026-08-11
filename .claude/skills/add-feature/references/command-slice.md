# Command Slice Templates

Files go in `src/server/Tnosc.EShop.Server.Application/<Context>/Commands/<Feature>/`.
Replace `<Context>` (e.g. `Catalog`), `<Aggregate>` (e.g. `Product`) and the use-case name.
Every file opens with the TNOSC header — omitted below for brevity, but it is mandatory.

## Command

A `public sealed record`. Carries raw primitives off the wire — **never** value objects or typed ids,
because the command is built by the Api layer before anything has been validated.

```csharp
using System;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Catalog.Commands.UpdateProductPrice;

/// <summary>
/// Reprices a product.
/// </summary>
/// <param name="ProductId">The identifier of the product to reprice.</param>
/// <param name="Amount">The new price amount.</param>
/// <param name="Currency">The three-letter ISO 4217 currency of the new price.</param>
public sealed record UpdateProductPriceCommand(
    Guid ProductId,
    decimal Amount,
    string? Currency) : ICommand;
```

- `ICommand` → handler is `ICommandHandler<TCommand>`, returns `Result`, endpoint responds `204`.
- `ICommand<TResponse>` → handler is `ICommandHandler<TCommand, TResponse>`, returns `Result<T>`.

Nullable `string?` for anything a value object will validate — the null check belongs to
`Sku.Create` / `Money.Create`, not to the command's type.

## Validator

`internal sealed`, same folder, auto-registered and run by `ValidationDecorator` before the handler.

**Structural checks only.** Required raw identifiers the domain never wraps in a value object, and
DTO shape. Formats, lengths, ranges and uniqueness are the domain's job — re-checking them here lets
the validator drift out of sync with the rule the aggregate actually enforces.

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Application.Validations;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Catalog.Commands.UpdateProductPrice;

/// <summary>
/// Structural validation only — the raw identifier the domain never wraps in a value object.
/// Amount and currency are validated by <see cref="Money.Create"/>.
/// </summary>
internal sealed class UpdateProductPriceCommandValidator : IValidator<UpdateProductPriceCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(
        UpdateProductPriceCommand request,
        CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (request.ProductId == Guid.Empty)
        {
            errors.Add(item: ProductErrors.IdRequired);
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
```

A slice whose command carries no bare identifiers needs **no validator at all** — don't add an empty one.

## Handler

`internal sealed`, primary constructor, `ValueTask<Result<T>>`.
Shape: **load → guard → delegate → persist → return unreinterpreted.**

### Creating an aggregate (uniqueness rule ⇒ go through the factory)

```csharp
[CacheTag(CacheTags.Catalog)]
internal sealed class CreateProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCommand, ProductId>
{
    /// <inheritdoc />
    public async ValueTask<Result<ProductId>> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        Result<Sku> sku = Sku.Create(value: command.Sku);

        if (sku.IsError)
        {
            return sku.Errors.ToArray();
        }

        Result<Money> price = Money.Create(amount: command.PriceAmount, currency: command.PriceCurrency);

        if (price.IsError)
        {
            return price.Errors.ToArray();
        }

        Result<Product> product = await ProductFactory.CreateAsync(
            repository: repository,
            sku: sku.Value,
            name: command.Name,
            price: price.Value,
            brandId: BrandId.From(value: command.BrandId),
            cancellationToken: cancellationToken);

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

### Mutating an existing aggregate

```csharp
[CacheTag(CacheTags.Catalog)]
internal sealed class UpdateProductPriceCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProductPriceCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        UpdateProductPriceCommand command,
        CancellationToken cancellationToken = default)
    {
        Product? product = await repository.GetByIdAsync(
            id: ProductId.From(value: command.ProductId),
            cancellationToken: cancellationToken);

        if (product is null)                                   // ✅ existence check
        {
            return ProductErrors.NotFound(productId: command.ProductId);
        }

        Result<Money> price = Money.Create(amount: command.Amount, currency: command.Currency);

        if (price.IsError)                                     // ✅ error propagation
        {
            return price.Errors.ToArray();
        }

        Result changed = product.ChangePrice(newPrice: price.Value);   // the entity owns the transition

        if (changed.IsError)
        {
            return changed.Errors.ToArray();
        }

        repository.Update(aggregate: product);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
```

## What must never appear in a handler

```csharp
if (product.IsDiscontinued) { … }              // ❌ → a guard inside Product.ChangePrice
if (command.Amount > 1000) { … }               // ❌ → a domain rule / strategy
var fee = method switch { … };                 // ❌ → IPaymentMethodStrategy + factory
if (await repo.GetBySkuAsync(…) is not null)   // ❌ → the domain factory owns uniqueness
_context.Products.Add(product);                // ❌ → repository contract, never DbContext
throw new ConflictException(…);                // ❌ → return an Error; exceptions are for infra
```

`NoBusinessBranchingTests` fails the build on any `if`, ternary, `switch` statement or `switch`
expression whose condition is not a null test, a `Result`/error-state test, or a cancellation check.

## Decorator attributes

Go **on the handler class**. The pipeline is registered centrally; the order is load-bearing:

```
Logging → Exception → Validation → Retry → CacheInvalidation → Transaction → Handler
```

| Attribute | When |
|---|---|
| `[CacheTag(CacheTags.Catalog)]` | The command mutates data that a `[Cacheable]` query caches. Evicts that tag after commit. |
| `[Retry(3)]` | The work is idempotent and can hit a transient failure. |
| `[Transactional]` | **Only** for multi-aggregate/multi-repository workflows, or a handler that calls `SaveChangesAsync` more than once. Not for the single-commit case above. |

**Cache tags are always constants, never string literals.** They live in
`Server.Shared/<Context>/CacheTags.cs` — add `using Tnosc.EShop.Server.Shared.Catalog;` to use them.
The handler that *invalidates* a tag lives in `Server.Application` and the handler that *populates*
it lives in `Server.Infrastructure.Persistence`; two literals in two projects drift silently, because
a mistyped tag doesn't fail the build, it just stops invalidating. `Server.Shared` is referenced by
both, which is exactly why the constant belongs there. See `.claude/rules/cache-tags.md`.

```csharp
namespace Tnosc.EShop.Server.Shared.Catalog;

/// <summary>
/// Cache tags shared by the Catalog bounded context's <c>[CacheTag]</c> handlers, so the write
/// handlers that invalidate and the query handlers that populate the cache cannot drift apart.
/// </summary>
public static class CacheTags
{
    /// <summary>
    /// Tag covering every cached Catalog query — invalidated by every Catalog write handler.
    /// </summary>
    public const string Catalog = "catalog";
}
```

## Domain events

Raised by the aggregate, not the handler — `product.AddDomainEvent(…)` inside `Create`/`ChangePrice`.
`UnitOfWork` converts them to outbox rows inside the same transaction on `SaveChangesAsync`. The
handler does nothing extra.

## Workflows (only when a handler outgrows the shape above)

If the use case spans several aggregates and steps, extract `I<X>Workflow` plus step services into
`Server.Application/<Context>/Workflows/`; the handler then injects the workflow and delegates in one
line, and takes `[Transactional]`. Bloat from *cross-cutting concerns* is a decorator's job instead.
