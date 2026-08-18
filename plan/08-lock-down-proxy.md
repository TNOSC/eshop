# Task 08 — Lock down the proxy

**Goal:** close the open proxy that [task 05](05-bff-proxy.md) deliberately left anonymous, without
breaking anonymous storefront browsing.

**Depends on:** [07](07-auth.md).

**Small task, high stakes.** Until it is done, `/bff/api/{**path}` is an unauthenticated tunnel to an
internal service that is otherwise unreachable from the internet.

---

## Files to edit

| File | Change |
|---|---|
| `Bff/BffProxy.cs` | `RequireAuthorization()` + an anonymous carve-out |
| `Bff/SameOriginRequirement.cs` (new) | the `X-Requested-With` CSRF guard |
| `.Client/Infrastructure/Api/RequestedWithHandler.cs` (new) | adds the header on every WASM call |
| `.Client/Extensions/ClientServiceCollectionExtensions.cs` | attach the handler on the WASM side |

---

## Two forwarders, not one

The API allows anonymous Catalog **reads**, and the storefront depends on that after WASM takes over —
`/products` must keep working for a signed-out visitor. But everything else must require the cookie.

```csharp
// Authenticated: everything under /bff/api
app.Map(pattern: BffRoutes.ApiCatchAll, handler: ForwardAsync)
   .RequireAuthorization()
   .DisableAntiforgery();

// Carve-out: anonymous GETs against the Catalog read endpoints only
app.MapGet(pattern: BffRoutes.CatalogCatchAll, handler: ForwardAsync)   // "/bff/api/catalog/{**path}"
   .AllowAnonymous();
```

`MapGet` on the more specific route wins over `Map` on the catch-all by ASP.NET Core's route precedence
(a literal segment beats a catch-all parameter). **Verify this rather than assuming it** — if precedence
goes the other way, anonymous browsing 401s.

Restricting the carve-out to **`GET`** is the important half. `POST /api/catalog/products` is an admin
write and must fall through to the authenticated forwarder.

---

## Antiforgery, and what replaces it

`app.UseAntiforgery()` is in the pipeline, and a WASM `fetch` carries no antiforgery token — so
`.DisableAntiforgery()` is unavoidable on the proxy. It needs a compensating defence, not just a comment.

**Two layers:**

1. **`SameSite=Lax` on the session cookie** (set in [task 07](07-auth.md)). This alone blocks cross-site
   `POST`s from carrying the cookie.
2. **Require `X-Requested-With: XMLHttpRequest`** on every `/bff/api` request. A cross-site HTML form
   cannot set a custom header; doing so from script triggers a CORS preflight, which the app does not
   answer. So the header is proof the request came from our own origin.

```csharp
// reject before forwarding
if (!context.Request.Headers.TryGetValue(key: "X-Requested-With", value: out StringValues requestedWith)
    || !StringValues.Equals(requestedWith, "XMLHttpRequest"))
{
    return Results.StatusCode(statusCode: StatusCodes.Status403Forbidden);
}
```

The header is added once, in a `DelegatingHandler` attached to the **WASM** registrations only, so no call
site has to remember:

```csharp
internal sealed class RequestedWithHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation(name: "X-Requested-With", value: "XMLHttpRequest");
        return base.SendAsync(request: request, cancellationToken: cancellationToken);
    }
}
```

The `configure` callback added to `AddEShopApiClients` in [task 04](04-api-client-infrastructure.md) is
exactly the hook for this — the WASM host passes `RequestedWithHandler`, the server host passes
`ServerAccessTokenHandler`.

**`POST /bff/logout` keeps antiforgery on.** It is a real form post from a Razor component, so it uses
`<AntiforgeryToken />` and needs no exemption.

---

## 401 handling on the client

An authenticated proxy means the client will eventually meet a 401 — the cookie expired and refresh
failed. `ApiResponseReader` already surfaces it as an `ApiProblem` with `Status = 401`; the page response
is to navigate to login:

```csharp
Navigation.NavigateTo(
    uri: $"bff/login?returnUrl={Uri.EscapeDataString(stringToEscape: Navigation.Uri)}",
    forceLoad: true);
return;
```

Same two rules as `RedirectToLogin` in [task 03](03-shell-and-layouts.md): **`forceLoad: true`**, and
**`return;` immediately after** (`BlazorDisableThrowNavigationException=true` means `NavigateTo` does not
throw during static SSR).

Do **not** treat 403 the same way. A 403 means authenticated-but-unpermitted — redirecting to login
produces an infinite loop where the user signs in successfully and is bounced straight back. Show a
"not permitted" message instead. The server's authorization chain is built specifically to distinguish
these two (see [`.claude/rules/authorization.md`](../.claude/rules/authorization.md)); do not collapse
them on the client.

---

## Definition of done

- [x] Signed out: `/products` and `/products/{id}` still work after WASM attaches. Verified live:
      `GET /bff/api/catalog/products?...` with `X-Requested-With` → **200**.
- [ ] Signed out: `curl` a non-catalog path such as `/bff/api/basket` → **401**, not a proxied response.
      Verified `RequireAuthorization()` now gates it (previously `AllowAnonymous` proxied straight
      through) — but this dev environment's Keycloak OIDC discovery is unreachable (a stale process
      unrelated to this change is squatting on `localhost:8080`), so the actual challenge path threw a
      500 instead of resolving to 401 during manual testing. Re-verify once Keycloak discovery is
      reachable.
- [ ] Signed out: `curl -X POST /bff/api/catalog/products` → **401**, i.e. the carve-out really is
      GET-only. Same environment blocker as above — confirmed the carve-out route is GET-only by
      inspection and that POST falls through to the authenticated route, but couldn't observe the
      final 401 for the same reason.
- [x] A request without `X-Requested-With` → **403**, even with a valid cookie. Verified live:
      `GET /bff/api/catalog/products` without the header → **403**.
- [ ] Signed in: every screen from tasks 06–07 still works (the handler is attached on the WASM side).
      Not exercised — needs a real browser sign-in, blocked by the same Keycloak discovery issue.
- [x] `dotnet build Tnosc.EShop.slnx` is clean.
