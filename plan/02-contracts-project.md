# Task 02 — Contracts project

**Goal:** a new `Tnosc.EShop.Client.Web.Contracts` project holding every wire type the client speaks —
DTOs, request records, route constants and the ProblemDetails shape. No behaviour.

**Depends on:** [01](01-guardrails.md).

---

## Why a separate project

`.Client` is `Microsoft.NET.Sdk.BlazorWebAssembly`. Anything referencing it drags WASM static-web-asset
targets along. These contracts are consumed by the typed clients (in `.Client`), potentially by the BFF
host, and by the test project — a plain `Microsoft.NET.Sdk` classlib is the only artefact all three can
take cheaply.

It also builds a compile-time wall: nothing in `Contracts` can accidentally take a dependency on
`Microsoft.FluentUI.*` or `Microsoft.AspNetCore.Components`, because the project references neither.

## Why the contracts are duplicated rather than shared

The server's request records are `internal sealed` in `Server.Api`; its DTOs live in
`Server.Application`. Referencing either would invert the dependency rules the architecture tests exist
to protect, and would pull EF Core into the WASM payload.

The repo already made this call once — `tests/server/Tnosc.EShop.Server.Tests.Acceptance/Contracts/`
restates routes and shapes deliberately, because *"a client that shared the server's route constants
could not catch a path changing underneath it"* ([`tests/CLAUDE.md`](../tests/CLAUDE.md)).

---

## Files to create

```
src/client/Tnosc.EShop.Client.Web/Tnosc.EShop.Client.Web.Contracts/
├─ Tnosc.EShop.Client.Web.Contracts.csproj      # plain Microsoft.NET.Sdk, ZERO PackageReferences
├─ AssemblyReference.cs                          # mirrors src/server/*/AssemblyReference.cs
├─ Common/
│  ├─ PagedResult.cs
│  ├─ ApiProblem.cs
│  └─ ApiRoutes.cs
├─ Catalog/
│  ├─ ProductSummary.cs   Product.cs   Category.cs
│  ├─ CreateProductRequest.cs   UpdateProductPriceRequest.cs   AdjustStockRequest.cs
│  └─ SearchProductsQuery.cs
├─ Identity/  CustomerSummary.cs   Customer.cs   CustomerAddress.cs
├─ Basket/    Basket.cs   BasketItem.cs   AddItemToBasketRequest.cs   ChangeBasketItemQuantityRequest.cs
└─ Ordering/  OrderSummary.cs   Order.cs   OrderLine.cs
```

Every file gets the TNOSC header, explicit `using`s, a file-scoped namespace, and one public type.

---

## The shapes

Mirroring the server exactly. `Status` and `Method` are plain `string` on the wire — do **not** invent
client-side enums, or an unrecognised server value becomes a deserialization crash instead of a display
oddity.

```csharp
public sealed record PagedResult<TItem>(
    IReadOnlyList<TItem> Items,
    int Page,
    int PageSize,
    long TotalCount,
    int TotalPages);

// Catalog
ProductSummary(Guid Id, string Sku, string Name, decimal PriceAmount, string PriceCurrency,
               int StockQuantity, string BrandName, string CategoryName)
Product(Guid Id, string Sku, string Name, string? Description, decimal PriceAmount, string PriceCurrency,
        int StockQuantity, Guid BrandId, Guid CategoryId, bool IsDiscontinued)
Category(Guid Id, string Name)
CreateProductRequest(string Sku, string Name, string? Description, decimal PriceAmount,
                     string PriceCurrency, int StockQuantity, Guid BrandId, Guid CategoryId)
UpdateProductPriceRequest(decimal Amount, string Currency)
AdjustStockRequest(int Delta)
SearchProductsQuery(string? Search, Guid? CategoryId, int Page, int PageSize)

// Identity
CustomerSummary(Guid Id, string Email, string FirstName, string LastName, bool IsActive)
Customer(Guid Id, string Email, string FirstName, string LastName, string? PhoneNumber, bool IsActive,
         Guid? DefaultAddressId, IReadOnlyList<CustomerAddress> Addresses)
CustomerAddress(Guid Id, string Street, string City, string PostalCode, string Country)

// Basket
Basket(Guid BasketId, Guid CustomerId, IReadOnlyList<BasketItem> Items,
       decimal? TotalAmount, string? TotalCurrency)
BasketItem(Guid ItemId, Guid ProductId, string Sku, string ProductName,
           decimal UnitPriceAmount, string UnitPriceCurrency, int Quantity)
AddItemToBasketRequest(Guid ProductId, int Quantity)
ChangeBasketItemQuantityRequest(int Quantity)

// Ordering
OrderSummary(Guid Id, string OrderNumber, string Status, decimal TotalAmount, string TotalCurrency,
             DateTime PlacedOnUtc, int LineCount)
Order(Guid Id, string OrderNumber, Guid CustomerId, string Status, decimal TotalAmount,
      string TotalCurrency, DateTime PlacedOnUtc, string ShippingStreet, string ShippingCity,
      string ShippingPostalCode, string ShippingCountry, IReadOnlyList<OrderLine> Lines)
OrderLine(Guid Id, Guid ProductId, string Sku, string ProductName, decimal UnitPriceAmount,
          string UnitPriceCurrency, int Quantity, decimal LineTotalAmount)
```

Note the server's `PagedResult<T>` serialises `TotalPages` as a computed property, so it arrives on the
wire and the client can simply receive it.

---

## `ApiRoutes` — no leading slash, ever

This is the single most important detail in the project. `new Uri(new Uri("https://x/bff/"), "/api/…")`
yields `https://x/api/…` — an absolute path **discards the base address's path segment**, so a leading
slash silently bypasses the BFF and the WASM client calls a URL that does not exist.

```csharp
public static class ApiRoutes
{
    public static class Catalog
    {
        public const string Products = "api/catalog/products";      // NO leading slash
        public const string Categories = "api/catalog/categories";

        public static string ProductById(Guid id) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Products}/{id}");

        public static string ProductPrice(Guid id) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Products}/{id}/price");

        public static string ProductStock(Guid id) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Products}/{id}/stock");
    }

    public static class Identity { /* api/identity/customers, …/me, …/me/profile, …/me/addresses */ }
    public static class Basket   { /* api/basket, api/basket/items */ }
    public static class Ordering { /* api/orders */ }
}
```

`CultureInfo.InvariantCulture` is not decoration — `CA1305` is a build error here, and a `Guid`
interpolated under a Turkish locale is still a real bug class.

Query-string building for `SearchProductsQuery` belongs here too, as a `static string SearchProducts(SearchProductsQuery query)`,
so no page hand-assembles a URL.

---

## `ApiProblem` — one record for both server shapes

The API returns two different ProblemDetails variants, and the client must handle both:

| Source | Shape |
|---|---|
| `Result` errors (`lib/Tnosc.Lib.Api/CustomResults.cs`) | `title` = error **code**, `detail` = description; for `ErrorType.Validation` only, an `errors` dictionary |
| Unhandled exceptions (`lib/Tnosc.Lib.Host/Middleware/GlobalExceptionHandler.cs`) | plus `errorCode` and `traceId` extensions |

```csharp
public sealed record ApiProblem(
    string? Type,
    string? Title,      // == error.Code, e.g. "Product.NotFound"
    int? Status,
    string? Detail,     // == error.Description
    string? Instance,
    IReadOnlyDictionary<string, string[]>? Errors,   // validation — keyed by ERROR CODE, not field name
    string? ErrorCode,  // unhandled-exception variant only
    string? TraceId);   // unhandled-exception variant only
```

> ⚠️ **`Errors` is keyed by error code, not field name.** `CustomResults.cs` builds
> `{ "Product.SkuAlreadyExists": ["…"] }`. This is not a mistake to work around here — task 09 bridges it
> to form fields with an explicit map. Just record the shape faithfully.

Add a `static ApiProblem FromStatus(int status)` factory for the case where a non-success response has no
parseable body at all.

---

## Wire into the solution

`.csproj` — genuinely empty of package references:

```xml
<Project Sdk="Microsoft.NET.Sdk" />
```

Everything else (TFM, nullable, analyzers, warnings-as-errors) comes from `Directory.Build.props`.

`Tnosc.EShop.slnx`, in the existing `/src/client/web/` folder:

```xml
<Project Path="src/client/Tnosc.EShop.Client.Web/Tnosc.EShop.Client.Web.Contracts/Tnosc.EShop.Client.Web.Contracts.csproj" />
```

Then add a `ProjectReference` to it from `Tnosc.EShop.Client.Web.Client.csproj`. The host project gets it
transitively.

> **Do not set `GenerateDocumentationFile`** on this project. Only the five `lib/` projects set it, so
> `CS1591` does not apply here. XML docs on the public records are still welcome for consistency — just
> not enforced.

---

## Definition of done

- [ ] The project exists, has zero `PackageReference` entries, and is in `.slnx`.
- [ ] `.Client` references it.
- [ ] Every record above exists, one per file, with the TNOSC header.
- [ ] **`grep -rn '"/api' src/client` returns nothing** — no route constant starts with a slash.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.
