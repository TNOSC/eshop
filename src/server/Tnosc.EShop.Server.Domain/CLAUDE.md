# Domain layer

Rich domain. This layer owns **every** business decision — if a rule is being decided anywhere else,
it is in the wrong place. No reference to EF Core, ASP.NET, Npgsql, or any outer layer.

## Layout

```
<Context>/<Aggregate>/{Product,ProductFactory,ProductId,Sku,Money,ProductErrors,IProductRepository}.cs
<Context>/<Aggregate>/Events/ProductCreatedDomainEvent.cs
```

| Thing | Shape |
|---|---|
| Aggregate / entity | `public sealed class Product : AggregateRoot<ProductId>` |
| Strongly-typed id | `public sealed record ProductId : GuidEntityId, IEntityId<ProductId, Guid>` |
| Value object | `public sealed record Money : ValueObject` |
| Domain factory | `public static class ProductFactory` |
| Error catalogue | `public static class ProductErrors` |
| Domain event | `public sealed record …DomainEvent : IDomainEvent` |
| Repository contract | `public interface IProductRepository : IRepository<Product, ProductId>` |

## Rules

- **No public setters** — `private set` or `init` only. The only parameterless constructor is the
  `private Product() { }` EF one.
- **Every state-changing method calls `IncrementVersion()`** before returning — including `Create`,
  which sets the initial fields. Skip it only for pure guards/queries that assign nothing.
- **Factories own creation rules that span the aggregate.** SKU uniqueness cannot be decided by one
  `Product` instance, so `ProductFactory.CreateAsync` consults `IProductRepository` and is the only
  way in from outside — `Product.Create` is `internal`. This is why repository contracts live *here*
  and not in Application: putting them there would force the uniqueness check back into a handler as
  a business `if`, which is banned.
- **Value objects validate in a static `Result<T> Create(...)` factory** with a non-public
  constructor. Enforced by an architecture test.
- **`Result` / `Result<T>` for expected failures**, never exceptions. Errors come from the
  aggregate's `*Errors` class so the code, wording and `ErrorType` are defined once.
- **Aggregates reference each other by id only** — no navigation properties across aggregates.
- **Strategy pattern for behavioural variation** (`IDiscountStrategy`, `IPaymentMethodStrategy`),
  chosen by a factory. Never a `switch` in a handler.
- **Domain events** are `sealed record`s carrying flat primitives, tagged with a stable
  `[DomainEventName("catalog.product-created.v1")]`. Names must be unique across the solution
  (architecture test) — version the suffix rather than renaming.
- Ids use `Guid.CreateVersion7()` so inserts stay sequential and the PK index does not fragment.
- Bounded contexts must not reference each other.

## Canonical aggregate

```csharp
public sealed class Product : AggregateRoot<ProductId>
{
    private Product() { /* EF. */ }

    public Sku Sku { get; private set; } = null!;
    public Money Price { get; private set; } = null!;

    internal static Result<Product> Create(Sku sku, string? name, Money price, …)
    {
        if (string.IsNullOrWhiteSpace(value: name))
        {
            return ProductErrors.NameRequired;
        }

        var product = new Product { Id = ProductId.New(), Sku = sku, Name = name, Price = price, … };
        product.IncrementVersion();
        product.AddDomainEvent(domainEvent: new ProductCreatedDomainEvent(
            Id: Guid.CreateVersion7(), OccurredOnUtc: DateTime.UtcNow, ProductId: product.Id.Value, …));

        return product;
    }

    public Result ChangePrice(Money newPrice)   // the entity owns the transition
    {
        if (IsDiscontinued)
        {
            return ProductErrors.Discontinued(productId: Id.Value);
        }

        Price = newPrice;
        IncrementVersion();
        AddDomainEvent(domainEvent: new ProductPriceChangedDomainEvent(…));

        return Result.Success();
    }
}
```

Unit-test every factory, entity transition, value object and strategy here — see `tests/CLAUDE.md`.
