---
description: "Client.Web.Client: the Blazor WebAssembly MVVM client — pages compose, components own a ViewModel and service"
applyTo: "src/client/Tnosc.EShop.Client.Web/Tnosc.EShop.Client.Web.Client/**"
---

# Client — the Blazor WebAssembly app (Store + Admin)

The interactive UI. Runs standalone under WASM (calling the BFF at its own origin's `/bff/` prefix)
and is also added as an interactive-WebAssembly render mode from the `.Web` host — both entry points
share every type in this project unchanged. See
[`blazor-client-mvvm.instructions.md`](blazor-client-mvvm.instructions.md) for the full
page/component/ViewModel/service split; this file is the map of where things live and the pieces the
rule doesn't cover.

## Layout

```
Features/Store/<Feature>/{Pages,Components,ViewModels,Services}   storefront slices
Features/Admin/<Feature>/{Pages,Components,ViewModels,Services}   back-office slices
Features/Shared/                                                  cross-feature presentational components
Infrastructure/Api/                                                typed IXxxApi clients + ApiClientNames
Infrastructure/Auth/Authorization/                                 Permissions · Roles · RolePermissions (mirrors Server.Shared)
Infrastructure/Errors/                                              ErrorCodeMessages, ValidationCodeFieldMap
Infrastructure/Validation/ClientValidation.cs                      shared Validate / ApplyFieldErrors helpers
Extensions/ClientServiceCollectionExtensions.cs                    AddEShopApiClients — registers every typed client + feature service
Routes.cs                                                          every route in the app, as constants/builders
```

A `Pages/` folder under a feature holds the routable `@page` components (composition only, per the
MVVM rule); `Components/` holds that feature's non-routable pieces; `ViewModels/` and `Services/` are
colocated 1:1 with the component that owns them, not centralized.

## Two hosts, one project

This project is built into two different apps and must stay usable by both:

- **Standalone WASM** (`Program.cs` here) — `AddEShopApiClients` is called with the WASM host's own
  origin + `/bff/`, and every typed client gets `RequestedWithHandler` (same-origin proof the BFF
  proxy checks, since a WASM caller has no server `HttpContext` to attach a bearer token from).
- **Interactive Server**, inside `Tnosc.EShop.Client.Web` (the host project, sibling directory) —
  registers the same clients against `eshop-host` directly, with `ServerAccessTokenHandler` attached
  instead.

Nothing in `Features/` or `Infrastructure/` may branch on which host it is running under — the two
hosts differ only in how `ClientServiceCollectionExtensions.AddEShopApiClients` is called, never in
component or service code.

## Routes and API paths — two separate tables, never inline

- **`Routes.cs`** (this project) is the only place an in-app navigation target or nav link is
  written. A page's own `@page` directive still repeats its route as a literal — Razor requires
  that — but nowhere else does.
- **`Tnosc.EShop.Client.Web.Contracts/Routes/ApiRoutes.cs`** (sibling `.Contracts` project) is the
  only place a BFF/API path is written. A `<Name>Service` builds its request through `ApiRoutes`,
  never a string literal, and an `IXxxApi` client method takes the already-built route.

## Permissions mirror `Server.Shared`, deliberately duplicated

`Infrastructure/Auth/Authorization/{Permissions,Roles,RolePermissions}.cs` are a **client-side copy**
of the server's permission vocabulary (`authorization.instructions.md`), used to hide/disable UI for
a permission the caller doesn't hold. This is a UX nicety, never the authorization boundary — every
mutating BFF call is still enforced server-side. Keep the two vocabularies spelled identically; a
mismatch just shows or hides the wrong button, it does not open a hole.

## Error handling — one vocabulary, two dictionaries

A `ClientProblem` returned by a service carries a server error code (`Product.NotFound`) or, for a
client-side validation failure, the shared `ClientValidation.ValidationErrorCode`. Two lookups act on
that code, never a component's own switch statement:

- `ErrorCodeMessages.Humanize(problem)` — the human-readable text shown in an `ErrorPanel`.
- `ValidationCodeFieldMap` (per feature, e.g. next to `CreateProductViewModel`) — maps a server
  validation error code to the ViewModel field it belongs to, consumed by
  `ClientValidation.ApplyFieldErrors`.

Add both when a new server error code needs to reach the UI; a code with no `ErrorCodeMessages` entry
falls back to a generic message rather than failing to build.

## Checklist

- [ ] Route added to `Routes.cs` (nav) or `ApiRoutes.cs` (API) — not inlined at the call site.
- [ ] A new permission-gated control checks `Permissions.*`/`Roles.*` here, matching
      `Server.Shared/Authorization/` exactly.
- [ ] A new server error code surfaced to the UI gets an `ErrorCodeMessages` entry (and a
      `ValidationCodeFieldMap` entry if it's field-level).
- [ ] Component/service code contains no branch on which host (WASM vs interactive-server) it runs
      under.
- [ ] Everything else — page/component/ViewModel/service split, `ComponentState`/`StatefulBoundary`,
      `ClientValidation` — follows `blazor-client-mvvm.instructions.md`.
