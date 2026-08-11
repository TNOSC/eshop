# Api layer

Minimal APIs only — no MVC controllers. **Must not reference Infrastructure.**

## Layout

```
<Context>/CatalogRoutes.cs                          route templates + OpenAPI tag, spelled once
<Context>/<Feature>/{CreateProductEndpoint,CreateProductRequest}.cs
Extensions/ApiExtensions.cs                         AddApiEndpoints(AssemblyReference.Assembly)
```

## Rules

- Endpoints are **`internal sealed class …Endpoint : IApiEndpoint`** with a single
  `void MapEndpoint(WebApplication app)` — enforced by an architecture test. They are discovered by
  assembly scan; there is no manual registration list.
- **Inject the closed handler interface directly** —
  `ICommandHandler<CreateProductCommand, ProductId>`, `IQueryHandler<TQuery, TResponse>`. No
  dispatcher, no mediator: it is reflection-free, startup-validated and compiler-checked.
- The HTTP handler is a `private static` method. Endpoints only ever consume `Result<T>` and map it
  through `ToHttp(...)` / `ToCreated(...)` / `CustomResults.Problem` — never `try`/`catch`, never a
  status-code decision of their own. `ErrorType` already determines the status.
- Request contracts carry a `ToCommand()` / `ToQuery()` mapper; the command/query types stay in
  Application. Never accept a command type straight off the wire.
- Route templates and the OpenAPI tag come from the context's `*Routes` constants, never inline
  strings, so the `Location` header and the map pattern cannot drift.
- Describe every endpoint for OpenAPI: `.WithName`, `.WithTags`, `.WithSummary`, `.WithDescription`
  and the `.Produces<T>(…)` set it can actually return.
- `using IResult = Microsoft.AspNetCore.Http.IResult;` — the alias disambiguates it from the domain
  `Tnosc.Lib.Domain.Results.IResult`.

```csharp
internal sealed class CreateProductEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: CatalogRoutes.Products, handler: HandleAsync)
           .WithName(endpointName: "CreateProduct")
           .WithTags(CatalogRoutes.Tag)
           .WithSummary(summary: "Create a product")
           .Produces<Guid>(statusCode: StatusCodes.Status201Created)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        CreateProductRequest request,
        ICommandHandler<CreateProductCommand, ProductId> handler,
        CancellationToken cancellationToken)
    {
        Result<ProductId> result = await handler.HandleAsync(
            command: request.ToCommand(), cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static productId =>
            Results.Created(uri: $"{CatalogRoutes.Products}/{productId.Value}", value: productId.Value));
    }
}
```

`ErrorType` → HTTP: `Validation` 400 · `Unauthorized` 401 · `Forbidden` 403 · `NotFound` 404 ·
`Conflict` 409 · `Failure`/`Unexpected` 500 · `Custom` → its `NumericType`.
