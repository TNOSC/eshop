# Endpoint Template

Files go in `src/server/Tnosc.EShop.Server.Api/<Context>/<Feature>/`.
`Server.Api` **must not reference Infrastructure** — it sees Application and Domain only.

## 1. Add the route constant first

Route templates and the OpenAPI tag live in the context's `*Routes` class, so a path is spelled once
and the `Location` header cannot drift from the map pattern.

```csharp
namespace Tnosc.EShop.Server.Api.Catalog;

internal static class CatalogRoutes
{
    /// <summary>The OpenAPI tag every Catalog endpoint is grouped under.</summary>
    public const string Tag = "Catalog";

    /// <summary>The products collection.</summary>
    public const string Products = "/api/catalog/products";

    /// <summary>A single product by identifier.</summary>
    public const string ProductById = $"{Products}/{{id:guid}}";

    /// <summary>A single product's price.</summary>
    public const string ProductPrice = $"{ProductById}/price";
}
```

## 2. Request contract

Next to the endpoint, with a `ToCommand()` / `ToQuery()` mapper. Never accept a command type straight
off the wire — the command belongs to Application and the request is the Api's own shape.

```csharp
/// <summary>The body of a create-product request.</summary>
public sealed record CreateProductRequest(
    string? Sku,
    string? Name,
    decimal PriceAmount,
    string? PriceCurrency,
    Guid BrandId)
{
    /// <summary>Maps the request onto its command.</summary>
    public CreateProductCommand ToCommand() =>
        new(Sku: Sku,
            Name: Name,
            PriceAmount: PriceAmount,
            PriceCurrency: PriceCurrency,
            BrandId: BrandId);
}
```

## 3. Endpoint

`internal sealed class …Endpoint : IApiEndpoint` — discovered by assembly scan, never registered by
hand. The HTTP handler is a `private static` method that injects the **closed handler interface**.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tnosc.EShop.Server.Application.Catalog.Commands.CreateProduct;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Catalog.CreateProduct;

/// <summary>
/// <c>POST /api/catalog/products</c> — adds a product to the catalogue.
/// </summary>
internal sealed class CreateProductEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: CatalogRoutes.Products, handler: HandleAsync)
           .WithName(endpointName: "CreateProduct")
           .WithTags(CatalogRoutes.Tag)
           .WithSummary(summary: "Create a product")
           .WithDescription(description: "Adds a new product. The SKU must be unique across the catalogue.")
           .Produces<Guid>(statusCode: StatusCodes.Status201Created)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        CreateProductRequest request,
        ICommandHandler<CreateProductCommand, ProductId> handler,
        CancellationToken cancellationToken)
    {
        Result<ProductId> result = await handler.HandleAsync(
            command: request.ToCommand(),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static productId =>
            Results.Created(uri: $"{CatalogRoutes.Products}/{productId.Value}", value: productId.Value));
    }
}
```

The `using IResult = Microsoft.AspNetCore.Http.IResult;` alias is required — it disambiguates from
the domain `Tnosc.Lib.Domain.Results.IResult` that `Result` implements.

## Result → HTTP

| Handler returns | Endpoint call | Success status |
|---|---|---|
| `Result<T>` (created) | `result.ToHttp(onSuccess: static x => Results.Created(uri: …, value: …))` | 201 |
| `Result<T>` (read) | `result.ToHttp(onSuccess: Results.Ok)` | 200 |
| `Result<T>` with a `Location` only | `result.ToCreated(location: static x => $"{Routes.X}/{x.Value}")` | 201 |
| `Result` (void command) | `result.ToHttp()` | 204 |
| `Result` (explicit code) | `result.ToHttp(successStatusCode: StatusCodes.Status202Accepted)` | as given |

Failures always go through `CustomResults.Problem`, which `ToHttp` applies for you. The `ErrorType`
the domain chose determines the status — the endpoint never picks one for a failure:

`Validation` 400 · `Unauthorized` 401 · `Forbidden` 403 · `NotFound` 404 · `Conflict` 409 ·
`Failure`/`Unexpected` 500 · `Custom` → its `NumericType`.

## Query endpoint

Identical shape; inject `IQueryHandler<TQuery, TResponse>` and build the query from route/query
parameters.

```csharp
public void MapEndpoint(WebApplication app) =>
    app.MapGet(pattern: CatalogRoutes.ProductById, handler: HandleAsync)
       .WithName(endpointName: "GetProductById")
       .WithTags(CatalogRoutes.Tag)
       .Produces<ProductDto>(statusCode: StatusCodes.Status200OK)
       .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound);

private static async Task<IResult> HandleAsync(
    Guid id,
    IQueryHandler<GetProductByIdQuery, ProductDto> handler,
    CancellationToken cancellationToken)
{
    Result<ProductDto> result = await handler.HandleAsync(
        query: new GetProductByIdQuery(ProductId: id),
        cancellationToken: cancellationToken);

    return result.ToHttp(onSuccess: Results.Ok);
}
```

## Banned in an endpoint

```csharp
using Tnosc.EShop.Server.Infrastructure.Persistence…   // ❌ Api must not reference Infrastructure
try { … } catch (Exception) { … }                      // ❌ decorators + global handler own this
if (result.FirstError.Type == ErrorType.NotFound)      // ❌ ToHttp/CustomResults already map it
return Results.BadRequest("SKU is required");          // ❌ validation belongs to the pipeline
app.MapPost("/api/catalog/products", …)                // ❌ use the *Routes constant
.HasPermission("catalog:write")                        // ❌ use the Permissions.* constant, not a literal
if (customer.Id != callerId) return Results.Forbid();  // ❌ resolve the caller from IUserContext instead
```
