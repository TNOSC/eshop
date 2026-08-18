# Task 04 — API client infrastructure

**Goal:** the typed-client machinery — `ApiResult<T>`, a response reader that handles both ProblemDetails
shapes, `ICatalogApi`/`CatalogApi`, and one registration extension called from **both** hosts.

**Depends on:** [03](03-shell-and-layouts.md).

---

## Files to create — in `.Client`

```
Tnosc.EShop.Client.Web.Client/
├─ Extensions/ClientServiceCollectionExtensions.cs   # AddEShopApiClients(...) — called from BOTH Program.cs
└─ Infrastructure/
   ├─ Api/
   │  ├─ ApiResult.cs           ApiResult{T}.cs
   │  ├─ ApiResponseReader.cs
   │  ├─ ApiClientNames.cs
   │  ├─ IdempotencyHeader.cs   # const string Name = "Idempotency-Key";
   │  └─ ICatalogApi.cs         CatalogApi.cs
   └─ Errors/
      └─ ErrorCodeMessages.cs   # error code -> human text
```

---

## `ApiResult<T>` — the client's `Result`

Mirrors the server's `Result` discipline: **no client method throws for a non-success status**, so pages
branch on data rather than catch exceptions.

```csharp
public sealed class ApiResult<TValue>
{
    public bool IsSuccess { get; }
    public TValue Value { get; }        // valid only when IsSuccess
    public ApiProblem? Problem { get; } // populated otherwise

    public static ApiResult<TValue> Success(TValue value);
    public static ApiResult<TValue> Failure(ApiProblem problem);
}
```

A non-generic `ApiResult` covers the 204 endpoints (price update, stock adjust, delete).

---

## `ApiResponseReader` — the interesting part

Four cases the server actually produces, all of which must be handled:

```csharp
public static async Task<ApiResult<TValue>> ReadAsync<TValue>(
    HttpResponseMessage response,
    CancellationToken cancellationToken)
{
    if (response.IsSuccessStatusCode)
    {
        return response.StatusCode is HttpStatusCode.NoContent
            ? ApiResult<TValue>.Success(value: default!)
            : ApiResult<TValue>.Success(
                value: (await response.Content.ReadFromJsonAsync<TValue>(
                    cancellationToken: cancellationToken))!);
    }

    ApiProblem problem =
        await ReadProblemAsync(response: response, cancellationToken: cancellationToken)
        ?? ApiProblem.FromStatus(status: (int)response.StatusCode);

    return ApiResult<TValue>.Failure(problem: problem);
}
```

| Case | Handling |
|---|---|
| **204 No Content** | `Success(default!)` — do **not** attempt to deserialize; `ReadFromJsonAsync` on an empty body throws |
| **201 with a bare `Guid` body** | `POST /api/catalog/products` and `POST /api/orders` return a **JSON scalar**, not `{ "id": … }`. `ReadFromJsonAsync<Guid>` is correct — there is no envelope |
| **`Result` error** | `title` = error code, `detail` = description, `errors` present only for `ErrorType.Validation` |
| **Unhandled exception** | additionally carries `errorCode` and `traceId` extensions |

`ReadProblemAsync` must not throw when the body is empty or is not JSON at all — a 502 from an
infrastructure hop has an HTML body. Wrap in a `try`/`catch (JsonException)` and fall back to
`ApiProblem.FromStatus`. (`CA1031` is already suppressed repo-wide, so a broad catch is acceptable here.)

---

## `ICatalogApi` and `CatalogApi`

The client class contains **no absolute URI** and **never contains the string `bff`** — the difference
between hosts is entirely in `BaseAddress`.

```csharp
internal sealed class CatalogApi(HttpClient httpClient) : ICatalogApi
{
    public async Task<ApiResult<PagedResult<ProductSummary>>> SearchProductsAsync(
        SearchProductsQuery query,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            requestUri: ApiRoutes.Catalog.SearchProducts(query: query),
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<PagedResult<ProductSummary>>(
            response: response,
            cancellationToken: cancellationToken);
    }
}
```

Methods for this task: `SearchProductsAsync`, `GetProductAsync`, `GetCategoriesAsync`. The three writes
(`CreateProductAsync`, `UpdateProductPriceAsync`, `AdjustStockAsync`) come in
[task 09](09-admin-catalog.md), where the idempotency-key parameter is designed.

The class uses a **primary constructor** (it is a plain class, not a component) and named arguments at
every call site, per [`.claude/rules/code-style.md`](../.claude/rules/code-style.md).

---

## Registration — one extension, two call sites

```csharp
// Extensions/ClientServiceCollectionExtensions.cs
public static IServiceCollection AddEShopApiClients(
    this IServiceCollection services,
    Uri baseAddress,
    Action<IHttpClientBuilder>? configure = null)
{
    IHttpClientBuilder catalog = services.AddHttpClient<ICatalogApi, CatalogApi>(
        configureClient: client => client.BaseAddress = baseAddress);
    configure?.Invoke(obj: catalog);
    // … the same for the other clients as they are added
    return services;
}
```

`.Client/Program.cs` (WASM):

```csharp
builder.Services.AddEShopApiClients(
    baseAddress: new Uri(uriString: builder.HostEnvironment.BaseAddress + "bff/"));
```

Host `Program.cs`:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ServerAccessTokenHandler>();          // created in task 05
builder.Services.AddEShopApiClients(
    baseAddress: new Uri(uriString: "https+http://eshop-host/"),
    configure: static builder => builder.AddHttpMessageHandler<ServerAccessTokenHandler>());
```

> ⚠️ **The `configure` callback exists specifically to avoid `ConfigureHttpClientDefaults`.** Attaching
> `ServerAccessTokenHandler` globally would put it on *every* `HttpClient` in the host, including the
> BFF's own downstream client and anything added later. Scope it to the typed clients.

**Service discovery already works.** `AddServiceDefaults()` calls `AddServiceDiscovery()` and
`ConfigureHttpClientDefaults(… AddStandardResilienceHandler())`
(`aspire/Tnosc.EShop.ServiceDefaults/Extensions.cs`), and the AppHost already has
`.WithReference(eshopHost)`. So `https+http://eshop-host/` resolves, and every client inherits the
resilience pipeline for free.

Note the **trailing slash** on both base addresses. Without it, `new Uri(base, "api/…")` replaces the last
path segment rather than appending.

---

## `ErrorCodeMessages`

A `FrozenDictionary<string, string>` from error code to human text. Never show a raw `Product.NotFound`
to a shopper — the codes are a machine vocabulary.

```csharp
public static string Humanize(ApiProblem problem) =>
    Messages.TryGetValue(key: problem.Title ?? string.Empty, value: out string? text)
        ? text
        : problem.Detail ?? "Something went wrong.";
```

The `problem.Detail` fallback is what keeps this safe: an unmapped code degrades to the server's own
description rather than to nothing.

---

## Definition of done

- [ ] `ApiResult<T>` and `ApiResult` exist; no API method throws on a non-success status.
- [ ] `ApiResponseReader` handles 204, bare-`Guid` bodies, both ProblemDetails variants, and a
      non-JSON body without throwing.
- [ ] `ICatalogApi` resolves from DI in **both** hosts (temporarily inject it into a placeholder page to
      prove it).
- [ ] `ServerAccessTokenHandler` is attached only to the typed clients, not via `ConfigureHttpClientDefaults`.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.

No page calls the API yet — that is [task 06](06-storefront-catalog.md), once the proxy exists.
