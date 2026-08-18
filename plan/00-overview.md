# Blazor Web Client — Overview

Turning `src/client/Tnosc.EShop.Client.Web/` from the stock Blazor Web App template into a real
two-audience application: an **admin console** (products, customers) and a **storefront** (browse,
basket, checkout), on Fluent UI v5, under `InteractiveAuto`, with bUnit component tests.

The backend is already finished — **37 endpoints across all five bounded contexts**, Keycloak-secured,
`Result` → RFC 7807. The gap is entirely on the client.

## Task order

Tasks **01 → 09 are strictly sequential**. Tasks 10, 11 and 12 can be done in any order once 09 is green.

| # | Task | Ends when |
|---|---|---|
| [01](01-guardrails.md) | Guardrails | The Razor toolchain is analyzer-clean before any feature code exists |
| [02](02-contracts-project.md) | Contracts project | `Tnosc.EShop.Client.Web.Contracts` builds and is referenced |
| [03](03-shell-and-layouts.md) | Shell and layouts | Both shells render; the rc.5 grep gate returns zero hits |
| [04](04-api-client-infrastructure.md) | API client infrastructure | `ICatalogApi` resolves in both hosts |
| [05](05-bff-proxy.md) | BFF proxy | `/bff/api/catalog/categories` returns real JSON in a browser |
| [06](06-storefront-catalog.md) | Storefront catalogue | Real products render, prerendered *and* after WASM attaches |
| [07](07-auth.md) | Auth | Sign in as admin and as customer; the nav differs |
| [08](08-lock-down-proxy.md) | Lock down the proxy | Anonymous reads still work; everything else needs the cookie |
| [09](09-admin-catalog.md) | Admin catalogue | Create / price / stock all round-trip; a repeated key replays |
| [10](10-skeletons.md) | Skeletons | Basket, checkout, orders, admin customers |
| [11](11-bunit-tests.md) | bUnit tests | `dotnet test tests/client/…` green |
| [12](12-polish-and-docs.md) | Polish and docs | ADR written, README no longer stale |

**Every task ends on a green `dotnet build Tnosc.EShop.slnx`.** Warnings are errors here, so a clean
build is the gate — not a suggestion.

## Architecture

### One app, two layouts

A single Blazor Web App. `/admin/*` uses `AdminLayout`, everything else uses `StoreLayout`. The split is
achieved by **one file** — `Features/Admin/_Imports.razor` containing `@layout AdminLayout` — which
applies to every page in that folder and its subfolders. No route-prefix sniffing, no per-page `@layout`.

> ⚠️ `@attribute [Authorize]` is **not** inherited through `_Imports.razor`. Only `@using`, `@layout`,
> `@inject` and `@namespace` propagate. Every admin page needs its own
> `@attribute [Authorize(Roles = "admin")]`.

### Three projects

```
src/client/Tnosc.EShop.Client.Web/
├─ Tnosc.EShop.Client.Web.Contracts/   plain Microsoft.NET.Sdk — DTOs, requests, ApiRoutes, ApiProblem
├─ Tnosc.EShop.Client.Web.Client/      WASM — layouts, pages, typed API clients
└─ Tnosc.EShop.Client.Web/             Web SDK — SSR shell, BFF proxy, OIDC
```

Everything renderable lives in `.Client` so identical code runs prerendered on the server and then in
WASM. The host project holds only the shell, the proxy and authentication.

### The BFF

**The API is not reachable from a browser.** `aspire/Tnosc.EShop.AppHost/Program.cs` gives
`WithExternalHttpEndpoints()` to `eshop-web` only, and `Server.Host/Program.cs` registers **no CORS
policy**. This is not a preference — a BFF is the only shape that works.

One typed client class serves both hosts. Only `BaseAddress` differs:

| Host | BaseAddress | Resulting URL | Auth |
|---|---|---|---|
| WASM | `{origin}/bff/` | `/bff/api/catalog/products` | cookie, automatic |
| Server (prerender) | `https+http://eshop-host/` | `/api/catalog/products` | `ServerAccessTokenHandler` reads `HttpContext` |

**Every `ApiRoutes` constant must be relative, with no leading slash.** `new Uri(new Uri("https://x/bff/"), "/api/…")`
yields `https://x/api/…` — the `/bff/` segment is silently dropped. This is the entire mechanism.

The server side calls the API **directly**, never its own `/bff`. A self-call during prerender is an
extra hop through its own pipeline, re-doing cookie auth for a request that already has an authenticated
`HttpContext`.

## Fluent UI v5 rc.5 — the skill file is wrong in five places

Verified against `Microsoft.FluentUI.AspNetCore.Components.xml` in the NuGet cache for
`5.0.0-rc.5-26219.1` — not the skill's prose.

| `.claude/skills/fluentui-blazor-usage` says | rc.5 actually has |
|---|---|
| `FluentTextField` | **`FluentTextInput`** (`Label`, `Placeholder`, `Required`, `TextInputType`, `Immediate`) |
| `FluentNumberField` | **`FluentNumberInput<TValue>`** (`Min`, `Max`, `Step`, `IsDecimal`, `StepButtons`) |
| "`IToastService` — removed, use `FluentToast` directly" | **`INotificationService` exists** — `ShowSuccessToastAsync`, `ShowErrorToastAsync`, `ShowWarningToastAsync`, `ShowMessageBarAsync`, … |
| `FluentNavItem Icon="…"` | **`IconRest`** / **`IconActive`** |
| `FluentNavCategory Label="…"` | **`Title`** |

**Why this matters more than a normal doc bug:** an unknown component name yields `RZ10012`
("found markup element with unexpected name"), which is a **warning that does not fail the build**. It
renders a silently broken unknown HTML element instead. This is the most likely silent failure in the
whole project, which is why task 03 ends with an explicit grep gate.

The skill *is* right about: `FluentNav` / `FluentNavItem` / `FluentNavCategory` / `FluentNavSectionHeader`
(not `FluentNavMenu`), `FluentLayout` / `FluentLayoutItem(Area)`, `FluentDataGrid` with
`Items` / `ItemsProvider` / `Pagination` / `EmptyContent`, `IDialogService.ShowDialogAsync<T>` with
`IDialogInstance` as a **cascading parameter** (not `IDialogContentComponent<T>`),
`FluentSelect<TOption, TValue>` with `SelectedItems`, `ButtonAppearance.Primary`, and the absence of
`FluentDesignTheme`. There is **no `FluentSearch`** — use `FluentTextInput TextInputType="TextInputType.Search"`.

## Conventions

From [`.claude/rules/code-style.md`](../.claude/rules/code-style.md), applied to every `.cs` file:

```csharp
// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------
```

- Explicit `using`s, System first, file-scoped namespace, one public type per file.
- **Named arguments at every call site.** More than two parameters ⇒ one per line.
- Explicit types, not `var`, except where the type is apparent.
- Every method returning `Task`/`ValueTask` is named `…Async`.
- **Primary constructors** — with one sanctioned exception: **Razor components take dependencies via
  `[Inject]` properties**, because the framework requires a parameterless component.
- **Code-behind `.razor.cs` partial classes for anything past trivial markup.** Analyzers behave
  predictably in real `.cs` files, and bUnit tests get a plain class to reason about.

## Contracts are duplicated from the server, deliberately

The server's request records are `internal sealed` in `Server.Api`; its DTOs live in
`Server.Application`. Referencing either would invert the dependency rules the architecture tests exist
to protect.

The repo already made this call once — `tests/server/Tnosc.EShop.Server.Tests.Acceptance/Contracts/`
restates routes and shapes because *"a client that shared the server's route constants could not catch a
path changing underneath it"* ([`tests/CLAUDE.md`](../tests/CLAUDE.md)). The web client follows the same
reasoning.

## Architecture tests do not scan the client — keep it that way

`tests/server/Tnosc.EShop.Server.Tests.Architecture.csproj` enumerates 13 explicit `ProjectReference`s
and `ConfigurationTests` names eight assemblies by hand. None are client projects, so nothing new goes
red.

**Do not add a client `ProjectReference` there.** `No_Constructor_Should_Inject_IConfiguration` would
fire immediately on any options binding, and `HandlerTests` / `NoBusinessBranchingTests` have no
meaningful reading for Razor components. If client rules are wanted later, they belong in a separate
`tests/client/Tnosc.EShop.Client.Tests.Architecture` with its own ruleset.

## Packages

Central Package Management is on — add the `PackageVersion` to `Directory.Packages.props`, then
reference the package **bare** with no `Version=`.

```xml
<!-- BFF: the web host runs the OIDC code flow; the browser never holds a token. -->
<PackageVersion Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="10.0.10" />
<!-- WASM has no shared framework, so AuthorizeView/CascadingAuthenticationState need this explicitly. -->
<PackageVersion Include="Microsoft.AspNetCore.Components.Authorization" Version="10.0.11" />
<!-- bUnit v2 is test-framework agnostic — no xunit dependency, so it composes with this repo's 2.9.3. -->
<PackageVersion Include="bunit" Version="2.9.0" />
```

`Aspire.Keycloak.Authentication` is **already pinned** at `13.4.6-preview.1.26319.6` — reuse it, do not
re-add. No YARP packages are needed (see [task 05](05-bff-proxy.md) for why the proxy is hand-written).

bUnit `2.9.0` ships a native `net10.0` dependency group built against `Microsoft.AspNetCore.Components`
10.0.10 — the exact version this solution uses.

## Verification (the whole thing, end to end)

```bash
dotnet build Tnosc.EShop.slnx                                   # warnings are errors
dotnet test  tests/client/Tnosc.EShop.Client.Tests.Unit
dotnet test  tests/server/Tnosc.EShop.Server.Tests.Architecture # must stay green
dotnet run   --project aspire/Tnosc.EShop.AppHost               # Docker required
```

From the Aspire dashboard, open `eshop-web` and walk:

1. **Anonymous** — the product grid renders, a detail page opens. Nothing 401s.
2. **`customer@eshop.local` / `Passw0rd!`** — `/admin` is refused, and the admin nav is absent both
   *before and after* WASM takes over. (This is the persisted-role-claim check; see task 07.)
3. Add to basket → `/checkout` → place order. `/orders` lists it.
4. **`admin@eshop.local` / `Passw0rd!`** — `/admin/products` pages against the server; create a product,
   change its price, adjust stock. Each raises a toast.
5. **Idempotency** — submit the create-product dialog, then submit again from the same open dialog.
   Exactly one product exists, and the second response is the replayed first one.
6. **Dev tools → Network** — requests go to `/bff/api/…` on the web app's own origin, and **no
   `Authorization` header and no token appear anywhere in the browser**. This is the BFF's whole point,
   and the one thing that cannot be verified from the server side.
