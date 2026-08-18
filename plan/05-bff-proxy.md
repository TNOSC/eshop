# Task 05 — BFF proxy

**Goal:** a `/bff/api/{**path}` forwarder on the web host that relays browser requests to
`https+http://eshop-host`, attaching the Bearer token server-side. Anonymous for now — locked down in
[task 08](08-lock-down-proxy.md).

**Depends on:** [04](04-api-client-infrastructure.md).

---

## Why hand-written rather than YARP

`AddServiceDefaults()` already applies `AddStandardResilienceHandler()` **and** `AddServiceDiscovery()`
through `ConfigureHttpClientDefaults` (`aspire/Tnosc.EShop.ServiceDefaults/Extensions.cs`). A named
`HttpClient` inherits both for free. YARP's `IHttpForwarder` uses its own `HttpMessageInvoker` and
**bypasses that pipeline entirely** — wiring resilience back in is more code than the forwarder itself.

YARP would also need route/cluster configuration bound from `IConfiguration`, which collides with
[`.claude/rules/configuration-options.md`](../.claude/rules/configuration-options.md), and two new
packages.

The forwarder is ~70 lines in one file and gives explicit control over the two things that actually
matter here: `Idempotency-Key` pass-through and `Authorization` injection. Per
[`.claude/rules/dependencies.md`](../.claude/rules/dependencies.md), state this reasoning in the commit
message.

---

## Files to create — in the host project

```
Tnosc.EShop.Client.Web/
├─ Bff/
│  ├─ BffRoutes.cs          # /bff/login, /bff/logout, /bff/user, /bff/api/{**path}
│  ├─ BffProxy.cs           # the forwarder
│  └─ BffProxyHeaders.cs    # hop-by-hop DENY-list
├─ Authentication/
│  └─ ServerAccessTokenHandler.cs
└─ Extensions/
   └─ BffEndpointExtensions.cs   # MapBffEndpoints()
```

---

## The forwarder

```csharp
internal static class BffProxy
{
    public static void MapProxy(WebApplication app) =>
        app.Map(pattern: BffRoutes.ApiCatchAll, handler: ForwardAsync)   // "/bff/api/{**path}"
           .AllowAnonymous()          // task 08 replaces this with RequireAuthorization + a carve-out
           .DisableAntiforgery();     // see below

    private static async Task ForwardAsync(
        HttpContext context,
        IHttpClientFactory factory,
        CancellationToken cancellationToken)
    {
        string? accessToken = await context.GetTokenAsync(tokenName: "access_token");

        HttpClient client = factory.CreateClient(name: ApiClientNames.Downstream);
        using var request = new HttpRequestMessage(
            method: new HttpMethod(method: context.Request.Method),
            requestUri: context.Request.Path.Value!["/bff/".Length..] + context.Request.QueryString);

        CopyRequestHeaders(source: context.Request, target: request);
        if (accessToken is not null)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(scheme: "Bearer", parameter: accessToken);
        }

        if (context.Request.ContentLength is > 0
            || context.Request.Headers.ContainsKey(key: "Transfer-Encoding"))
        {
            request.Content = new StreamContent(content: context.Request.Body);
            CopyContentHeaders(source: context.Request, target: request.Content);
        }

        using HttpResponseMessage response = await client.SendAsync(
            request: request,
            completionOption: HttpCompletionOption.ResponseHeadersRead,
            cancellationToken: cancellationToken);

        context.Response.StatusCode = (int)response.StatusCode;
        CopyResponseHeaders(source: response, target: context.Response);
        await response.Content.CopyToAsync(
            stream: context.Response.Body,
            cancellationToken: cancellationToken);
    }
}
```

`HttpCompletionOption.ResponseHeadersRead` + `CopyToAsync` streams the body rather than buffering it —
relevant the first time someone returns a large paged result.

---

## Header copying must be a DENY-list

**This is the security- and correctness-critical detail of the whole task.**

`CopyRequestHeaders` copies **everything except** a small deny-list — never an allow-list. An allow-list
silently drops `Idempotency-Key`, and then every `POST /api/catalog/products` returns 400
`Idempotency.KeyMissing`, which looks like a server bug rather than a proxy bug.

```csharp
// BffProxyHeaders.cs
private static readonly FrozenSet<string> RequestDenyList =
    new[] { "Host", "Cookie", "Connection", "Keep-Alive", "Proxy-Authorization",
            "TE", "Trailer", "Transfer-Encoding", "Upgrade", "Authorization" }
        .ToFrozenSet(comparer: StringComparer.OrdinalIgnoreCase);
```

- **`Cookie`** is denied deliberately — the session cookie is the BFF's business and must not reach the
  API.
- **`Authorization`** is denied so a caller cannot smuggle their own token past the proxy; the proxy sets
  it itself, after the deny-list runs.
- **`Transfer-Encoding`** is denied on the response side too, or Kestrel and `HttpClient` disagree about
  framing.

Content headers (`Content-Type`, `Content-Length`) live on `request.Content.Headers`, not
`request.Headers` — hence the separate `CopyContentHeaders`. Getting this wrong produces a 415 on every
POST.

---

## `DisableAntiforgery()`

`app.UseAntiforgery()` is already in the host pipeline. A `POST /bff/api/...` issued by WASM carries no
antiforgery token and would 400.

Disabling it here is safe **only because task 08 adds a compensating CSRF defence** (an
`X-Requested-With: XMLHttpRequest` requirement plus `SameSite=Lax` cookies). Do not ship task 05 to
anything public on its own.

---

## The downstream client

```csharp
builder.Services.AddHttpClient(
    name: ApiClientNames.Downstream,
    configureClient: static client => client.BaseAddress = new Uri(uriString: "https+http://eshop-host/"));
```

Service discovery resolves `eshop-host`, and `ConfigureHttpClientDefaults` gives it the standard
resilience handler.

> **`AddStandardResilienceHandler()` retries transient failures on every method, including `POST`.**
> That retry happens *below* the point where `Idempotency-Key` is set, so every Polly attempt carries the
> same key — which is exactly why the API demands one. Break the header pass-through above and a single
> transient 503 during checkout produces two orders.

---

## `ServerAccessTokenHandler`

Used by the *typed clients* on the server side (task 04 registers it), not by the proxy — the proxy sets
the header itself.

```csharp
internal sealed class ServerAccessTokenHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpContext? context = httpContextAccessor.HttpContext;
        if (context?.User.Identity?.IsAuthenticated == true)
        {
            string? accessToken = await context.GetTokenAsync(tokenName: "access_token");
            if (accessToken is not null)
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(scheme: "Bearer", parameter: accessToken);
            }
        }

        return await base.SendAsync(request: request, cancellationToken: cancellationToken);
    }
}
```

Until task 07 wires OIDC, `GetTokenAsync` returns null and the handler is a no-op. That is fine —
Catalog reads are anonymous, so [task 06](06-storefront-catalog.md) works without a token.

---

## Pipeline placement

`MapBffEndpoints()` goes **after** `app.UseAntiforgery()` and **before** `app.MapRazorComponents<App>()`
in the host `Program.cs`. From task 07 it must also sit after `UseAuthentication()`/`UseAuthorization()`,
since `GetTokenAsync` reads the authenticated `HttpContext`.

---

## Definition of done

- [ ] `GET https://<web-app>/bff/api/catalog/categories` returns the real category JSON in a browser.
- [ ] `GET /bff/api/catalog/products?page=1&pageSize=5` returns a real `PagedResult`, query string intact.
- [ ] The header copy is a **deny-list**; `Idempotency-Key` demonstrably survives (send one with `curl`
      and confirm the API does not answer `Idempotency.KeyMissing`).
- [ ] No `Cookie` header reaches the API.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.
