# Query Slice Templates

A query is split across **two** projects — that split is enforced by an architecture test:

| Part | Project | Folder |
|---|---|---|
| Query + DTO | `Server.Application` | `<Context>/Queries/<Feature>/` |
| **Handler** + read model | `Server.Infrastructure.Persistence` | `<Context>/Queries/`, `<Context>/ReadModels/` |

Every file opens with the TNOSC header — omitted below, but mandatory.

## Query

`public sealed record`, raw primitives only.

```csharp
using System;
using Tnosc.Lib.Application.Queries;

namespace Tnosc.EShop.Server.Application.Catalog.Queries.GetProductById;

/// <summary>
/// Retrieves a single product by identifier.
/// </summary>
/// <param name="ProductId">The identifier of the product to retrieve.</param>
public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto>;
```

For a cached query, mark the properties that form the cache key:

```csharp
public sealed record SearchProductsQuery(
    [property: CacheKey] string? SearchTerm,
    [property: CacheKey] Guid? CategoryId,
    [property: CacheKey] int Page,
    [property: CacheKey] int PageSize) : IQuery<PagedResult<ProductSummaryDto>>;
```

## DTO

`public sealed record`, in the **Application** project next to its query, flat primitives only.
Never a domain entity, never a typed id, never a value object.

```csharp
public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal PriceAmount,
    string PriceCurrency,
    int StockQuantity,
    Guid BrandId,
    Guid CategoryId,
    bool IsDiscontinued);
```

Paged reads return `PagedResult<TDto>` (from `Tnosc.Lib.Application.Queries`).

## Read model

`internal sealed : IReadModel` in `…Persistence/<Context>/ReadModels/`, with an
`IEntityTypeConfiguration<>` mapping it to the same table the write model owns.

The read model deliberately does **not** reuse the aggregate: the read context maps read models only,
and a flat shape keeps projections translatable without dragging owned types and value converters
onto the query path.

```csharp
internal sealed class ProductReadModel : IReadModel
{
    /// <summary>Gets the product's identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the product's stock-keeping unit.</summary>
    public string Sku { get; init; } = null!;

    /// <summary>Gets the product's current price amount.</summary>
    public decimal PriceAmount { get; init; }
}
```

## Query handler — LINQ projection

`internal sealed`, takes `EShopReadDbContext`, projects straight into the DTO.

```csharp
internal sealed class GetProductByIdQueryHandler(EShopReadDbContext context)
    : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    /// <inheritdoc />
    public async ValueTask<Result<ProductDto>> HandleAsync(
        GetProductByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ProductDto? product = await context.Set<ProductReadModel>()
            .Where(predicate: readModel => readModel.Id == query.ProductId)
            .Select(selector: readModel => new ProductDto(
                Id: readModel.Id,
                Sku: readModel.Sku,
                Name: readModel.Name,
                PriceAmount: readModel.PriceAmount,
                PriceCurrency: readModel.PriceCurrency))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (product is null)
        {
            return ProductErrors.NotFound(productId: query.ProductId);
        }

        return product;
    }
}
```

No `AsNoTracking()` call is needed — the read context is configured for it.

## Query handler — raw SQL

Use raw SQL when the query **joins several tables and projects a subset of their columns**. Rules:

- Every value reaches the database as an `NpgsqlParameter`. **Never interpolate into the SQL text.**
- Value converters do not apply to raw SQL, so the row type declares `Guid`, not `ProductId`.
- `COUNT(*) OVER ()` carries the unpaged total on every row — one round trip answers page and count.
- Clamp paging inputs.
- The row type (`ProductSearchRow`) is `internal sealed`, lives beside the handler, and is mapped to
  the public DTO before returning.

```csharp
private const string SearchSql = """
    SELECT p.id             AS "Id",
           p.sku            AS "Sku",
           b.name           AS "BrandName",
           COUNT(*) OVER () AS "TotalCount"
    FROM catalog.products p
    INNER JOIN catalog.brands b ON b.id = p.brand_id
    WHERE (@search IS NULL OR p.name ILIKE @search)
    ORDER BY p.name, p.id
    OFFSET @skip LIMIT @take
    """;

int page = Math.Max(val1: query.Page, val2: 1);
int pageSize = Math.Clamp(value: query.PageSize, min: 1, max: MaxPageSize);

List<ProductSearchRow> rows = await context.Database
    .SqlQueryRaw<ProductSearchRow>(
        sql: SearchSql,
        parameters:
        [
            Parameter(name: "search", type: NpgsqlDbType.Text, value: ToLikePattern(term: query.SearchTerm)),
            Parameter(name: "skip", type: NpgsqlDbType.Integer, value: (page - 1) * pageSize),
            Parameter(name: "take", type: NpgsqlDbType.Integer, value: pageSize),
        ])
    .ToListAsync(cancellationToken: cancellationToken);

private static NpgsqlParameter Parameter(string name, NpgsqlDbType type, object? value) =>
    new(parameterName: name, parameterType: type) { Value = value ?? DBNull.Value };
```

## Caching

`[Cacheable(seconds)]` on the handler class, with `[CacheKey]` on the query properties that vary the
result. Every command that mutates the cached data carries the matching `[CacheTag(...)]`, or the
cache goes stale. `CacheInvalidationDecorator` sits outside `Transaction`, so eviction happens after
commit.

**Both sides must resolve the same constant** — `Server.Shared/<Context>/CacheTags.cs`, never a
string literal on either side. The populating query handler lives here in
`Server.Infrastructure.Persistence`; the invalidating command handler lives in `Server.Application`.
A literal mistyped in one of them doesn't fail the build, it just silently stops invalidating.
See `.claude/rules/cache-tags.md`.

```csharp
using Tnosc.EShop.Server.Shared.Catalog;

[Cacheable(300)]
[CacheTag(CacheTags.Catalog)]
internal sealed class GetCategoriesQueryHandler(EShopReadDbContext context)
    : IQueryHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryDto>>
```

## Banned in a query handler

```csharp
IProductRepository repository        // ❌ reads never go through the write model (arch test)
EShopWriteDbContext context          // ❌ use EShopReadDbContext
return product;                      // ❌ never return a domain entity — project to a DTO
$"WHERE name = '{query.Name}'"       // ❌ parameterise
if (product.Price > 100) { … }       // ❌ business branching
```
