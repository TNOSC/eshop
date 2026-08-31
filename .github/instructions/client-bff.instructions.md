---
description: "Client.Web: the Blazor host and BFF — typed API clients, auth wiring, and the render-mode boundary"
applyTo: "src/client/Tnosc.EShop.Client.Web/Tnosc.EShop.Client.Web/**"
---

# Web — the server host: BFF + interactive-server render

The one process a browser talks to. Two responsibilities live here and nowhere else:

1. **Backend-for-frontend** — owns the Keycloak OIDC login/logout, holds the real access token
   server-side, and proxies API calls for the browser. Built on the identity-provider-agnostic
   `Tnosc.Lib.Web.Bff` framework project; this project supplies only the eShop-specific pieces.
2. **Interactive Server render** of the `.Client` project's components (plus WebAssembly render mode
   for the same components) — see the sibling `client-blazor.instructions.md` for
   page/component conventions. Nothing under `Components/` here holds feature markup; the routable
   tree lives in `.Client`.

## Layout

```
Authentication/          KeycloakRoleClaimsTransformation, ServerAccessTokenHandler, PersistingRevalidatingAuthenticationStateProvider
Bff/EShopBffRoutes.cs     eShop's own addition to Tnosc.Lib.Web.Bff.BffRoutes — the anonymous Catalog-read carve-out
Extensions/               WebAuthenticationExtensions (AddEShopBffAuthentication), BffEndpointExtensions (MapBffEndpoints)
Options/OidcOptions.cs    settings bound from appsettings.json (see configuration-options.md)
Components/App.razor, Routes.razor, _Imports.razor   Razor host shell only — no feature markup
```

## Why the BFF pattern, and what never changes

The browser never holds an access token. `ServerAccessTokenHandler` reads it from the authenticated
`HttpContext` and attaches it to the *typed* API clients (`AddEShopApiClients` registration in
`Program.cs`); the BFF's own downstream proxy client is registered **separately**
(`ApiClientNames.Downstream`) specifically so `ServerAccessTokenHandler` is never attached to it — the
proxy sets `Authorization` itself, from the token it reads directly off the incoming request. Do not
merge these two client registrations; that was a deliberate split, not an oversight.

**Anonymous Catalog reads are the only carve-out.** `EShopBffRoutes.CatalogCatchAll` is a `GET`-only
pattern so a signed-out visitor can browse the storefront before WASM even loads. It is a business
decision that belongs in *this* project — `Tnosc.Lib.Web.Bff.BffProxy` stays generic and takes the
pattern as a parameter; it does not know what "Catalog" or "anonymous read" mean.

## Pipeline order is load-bearing

`Program.cs`: `UseAuthentication()` → `UseAuthorization()` → `UseAntiforgery()` → `MapBffEndpoints()`
→ `MapRazorComponents<App>()`. `MapBffEndpoints` must run after auth/antiforgery (the proxy and
`UserInfoEndpoint` read the authenticated `HttpContext`) and before the component tree is mapped.
Follow the authentication-pipeline ordering already documented in
[`authorization.instructions.md`](authorization.instructions.md) for the same reason
that rule states it there — this host is the one place both concerns (BFF pipeline order, permission
claims) meet.

## Rendering — Server and WebAssembly, same component tree

`AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents()` at registration,
`.AddInteractiveServerRenderMode().AddInteractiveWebAssemblyRenderMode()` at mapping. This host never
defines its own render-mode boundary inside a page — that is set per-page in `.Client`, never here
(see `project_blazor_rendermode_boundary` — `Routes.razor` in this project must never carry an
interactive `@rendermode`).

Interactive Server rendering runs FluentUI components server-side, which is why this project also
registers a plain `AddHttpClient()` distinct from the typed eShop clients — it is FluentUI's own
requirement, not part of the BFF wiring above.

## Checklist

- [ ] A new downstream call goes through the typed `IXxxApi` clients (token attached automatically);
      only the proxy itself uses `ApiClientNames.Downstream`.
- [ ] A new anonymous route is added to `EShopBffRoutes` explicitly — nothing is anonymous by default.
- [ ] `Program.cs` ordering (`UseAuthentication` → `UseAuthorization` → `UseAntiforgery` →
      `MapBffEndpoints` → `MapRazorComponents`) is preserved.
- [ ] `Components/Routes.razor` carries no `@rendermode` — set it per-page in `.Client`.
- [ ] New settings go through an `Options` class per `configuration-options.instructions.md`
      (see `Options/OidcOptions.cs` for the existing shape), not a raw `IConfiguration` read here.
