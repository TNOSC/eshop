---
description: "Server.Infrastructure.Persistence: EF configuration, repositories, query handlers and read models — dumb and policy-free"
applyTo: "src/server/Tnosc.EShop.Server.Infrastructure.Persistence/**"
---

# Infrastructure — Persistence

Technical execution only. Catch technical exceptions and rethrow them as application exceptions;
make **no** policy decisions and carry **no** business control flow. The query side of CQRS lives
here — that is deliberate, not a layering slip.

## Layout

```
<Context>/CatalogSchema.cs                 schema + table name constants
<Context>/Configurations/ProductConfiguration.cs
<Context>/Queries/{GetProductByIdQueryHandler,SearchProductsQueryHandler,ProductSearchRow}.cs
<Context>/ReadModels/ProductReadModel.cs
<Context>/Repositories/ProductRepository.cs
Contexts/{EShopWriteDbContext,EShopReadDbContext}.cs · DesignTime/ · Migrations/ · Extensions/
```

Everything here is `internal sealed`.

## Write side

- `ProductRepository : RepositoryBase<Product, ProductId>, IProductRepository`, taking
  `EShopWriteDbContext`. Methods express business intent (`GetBySkuAsync`), not query mechanics.
- EF configuration in `IEntityTypeConfiguration<T>`: `ToTable(name, schema)`, explicit `snake_case`
  column names, value objects as `OwnsOne`, `builder.ConfigureAggregateRootColumns()` last.
- **No `HasConversion` for typed ids** — `EntityIdConventions` registers one converter per id type as
  pre-convention model configuration, which also covers foreign keys.
- Aggregates hold no navigation properties, but the FK constraints still belong in the database:
  `HasOne<Brand>().WithMany().HasForeignKey(…).OnDelete(DeleteBehavior.Restrict)`.
- `UnitOfWork` stamps auditing and converts raised domain events into outbox rows **inside the same
  transaction**; `OutboxProcessor` then delivers them using `FOR UPDATE SKIP LOCKED`.

## Read side

- Query handlers take **`EShopReadDbContext`** — its `SaveChanges`/`SaveChangesAsync` are sealed
  overrides that always throw, so read-only is a guarantee rather than a convention.
- Query handlers **must not reference `I*Repository`** — reads never go through the write model
  (architecture test).
- Project a **read model** (flat primitives, no typed ids, no value objects, `IReadModel`) into the
  Application-layer DTO. Never reuse the write aggregate as a read model, and never leak a domain
  entity out of a handler.
- **Raw SQL for multi-table joins** — that is what it is reserved for. Every value goes through an
  `NpgsqlParameter`; nothing is interpolated into the SQL text. Value converters do not apply to raw
  SQL, so raw-SQL row types declare `Guid`, not `ProductId`. `COUNT(*) OVER ()` carries the unpaged
  total so one round trip answers both the page and its count.
- Clamp paging (`Math.Clamp(value: query.PageSize, min: 1, max: MaxPageSize)`).

```csharp
internal sealed class GetProductByIdQueryHandler(EShopReadDbContext context)
    : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    public async ValueTask<Result<ProductDto>> HandleAsync(
        GetProductByIdQuery query, CancellationToken cancellationToken = default)
    {
        ProductDto? product = await context.Set<ProductReadModel>()
            .Where(predicate: readModel => readModel.Id == query.ProductId)
            .Select(selector: readModel => new ProductDto(Id: readModel.Id, Sku: readModel.Sku, …))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (product is null)
        {
            return ProductErrors.NotFound(productId: query.ProductId);
        }

        return product;
    }
}
```

## Migrations

Two contexts exist, so `--context` is always required; only the write context has migrations.

```bash
dotnet ef migrations add <Name> --context EShopWriteDbContext \
  --project src/server/Tnosc.EShop.Server.Infrastructure.Persistence \
  --startup-project src/server/Tnosc.EShop.Server.Host
```

`EShopWriteDbContextFactory` supplies the design-time connection string (env
`ConnectionStrings__eshopdb`, falling back to a local default) because `dotnet ef` runs outside
Aspire. At runtime, migrations apply on startup only when `Persistence:ApplyMigrationsOnStartup` is
`true`. Aspire's Postgres uses `WithDataVolume()`, so a schema change may need the volume dropped.

## DI

`InfrastructurePersistenceExtensions.AddInfrastructurePersistence` registers both contexts against
the `eshopdb` Aspire connection, calls `AddPersistence<TWrite, TRead>`, then Scrutor-scans
repositories (`AsImplementedInterfaces`, so both `IProductRepository` and the closed `IRepository<,>`
resolve) and query handlers, and applies the query decorator chain — innermost first:
`Retry` → `Cacheable` → `Exception` → `Logging`.

**Settings from `appsettings.json`** — if this layer needs configuration bound from JSON (e.g., a
batch size, a timeout, a feature flag), define a `<Feature>Options` class here and bind it in the
`AddXxx` extension method. See [`configuration-options.instructions.md`](configuration-options.instructions.md)
for the pattern: the class never leaves the `AddXxx` method, consumers inject the plain `TOptions` class directly,
and validation happens at app startup.

Query handlers are covered by integration tests against real Postgres — see `tests.instructions.md`.
