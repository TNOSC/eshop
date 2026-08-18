# Task 12 — Polish and docs

**Goal:** remove the prerender double-fetch, set the Release knobs, and write down the decisions so the
next person does not have to re-derive them.

**Depends on:** [09](09-admin-catalog.md). Can be done alongside [10](10-skeletons.md) and
[11](11-bunit-tests.md).

---

## 1. Kill the prerender double-fetch

Under `InteractiveAuto`, an anonymous prerendered page fetches **twice**: once on the server during
prerender, once when WASM boots. Authenticated pages already avoid it via `prerender: false`
([task 10](10-skeletons.md)), but `/products` and `/products/{id}` keep prerendering on purpose — that is
the SEO those two pages exist to get.

Pass the first payload through `PersistentComponentState`, the same channel the auth state already
travels on:

```csharp
protected override async Task OnInitializedAsync()
{
    if (ApplicationState.TryTakeFromJson(key: StateKey, instance: out PagedResult<ProductSummary>? cached))
    {
        _products = cached;
        return;
    }

    _products = (await CatalogApi.SearchProductsAsync(query: _query, cancellationToken: _cts.Token)).Value;
    _persistingSubscription = ApplicationState.RegisterOnPersisting(callback: PersistAsync);
}
```

.NET 10's declarative `[PersistentState]` attribute is the terse form; the explicit
`RegisterOnPersisting` + `TryTakeFromJson` pair above is the portable one. Either is fine — pick one and
be consistent.

**Symptom if skipped:** doubled API load on the busiest page, plus a visible content flash as the grid
re-renders with identical data. Measurable in dev tools: two `GET /api/catalog/products` per page load.

## 2. Release knobs for the WASM payload

`Microsoft.FluentUI.AspNetCore.Components.Icons` is large. In the `.Client` csproj:

```xml
<PublishTrimmed>true</PublishTrimmed>
<BlazorWebAssemblyLoadAllGlobalizationData>false</BlazorWebAssemblyLoadAllGlobalizationData>
```

> ⚠️ The globalization setting interacts with currency formatting. This is exactly why
> [task 06](06-storefront-catalog.md) formats money as `"N2"` + an explicit currency code rather than
> `"C"` — `"C"` depends on the globalization data this flag drops. If any `"C"` format crept in, it will
> surface here as a Release-only bug, which is the worst kind to find.

Verify a Release publish actually runs before calling this done. Trimming plus a Razor class library is a
combination that reliably finds something.

## 3. `src/client/CLAUDE.md`

A scoped `CLAUDE.md` alongside the existing ones (`lib/`, each `src/server/*`, `tests/`). It should be
short and cover only what is not derivable from the code:

- **The three-project split** and what belongs in each. Everything renderable lives in `.Client`; the host
  holds only the shell, the BFF and OIDC.
- **`ApiRoutes` constants are relative, never leading-slash** — and why (the `new Uri(base, "/x")`
  base-path-discard rule).
- **Components take dependencies via `[Inject]` properties**, the one sanctioned exception to the
  repo's primary-constructor rule, because the framework requires a parameterless component.
- **Logic goes in `.razor.cs` code-behind** past trivial markup.
- **`@attribute [Authorize]` is not inherited through `_Imports.razor`** — repeat it on every admin page.
- **The Fluent UI v5 rc.5 corrections** — point at [`00-overview.md`](00-overview.md)'s table, and say
  plainly that the `fluentui-blazor-usage` skill is wrong on `FluentTextField`, `FluentNumberField`,
  `IToastService`, `FluentNavItem.Icon` and `FluentNavCategory.Label`.

## 4. An ADR

`docs/decisions/` currently stops at ADR-016 and is entirely backend. Add ADR-017 recording the three
decisions someone will otherwise reopen:

1. **BFF over browser-held tokens.** Not a preference — `Server.Host` registers no CORS policy and
   `eshop-host` has no external endpoint, so a WASM page cannot reach the API directly. The BFF is the
   only shape that works, and it has the side benefit that no token ever reaches the browser.
2. **A hand-written forwarder over YARP.** `AddServiceDefaults()` already supplies service discovery and
   `AddStandardResilienceHandler()` through `ConfigureHttpClientDefaults`; YARP's `IHttpForwarder` uses
   its own `HttpMessageInvoker` and bypasses that pipeline. Re-adding resilience would be more code than
   the forwarder. Record the deny-list header rule here too — it is the part that silently breaks
   idempotency if someone "tidies" it into an allow-list.
3. **Contracts duplicated rather than shared with the server.** Same reasoning already recorded for
   `Tests.Acceptance/Contracts` — a client that shared the server's route constants could not catch a
   path changing underneath it.

Worth also recording: **the code-keyed validation bridge**, since `ValidationCodeFieldMap` looks like
redundant duplication until you know the server keys `errors` by error code rather than field name.

## 5. Fix the stale README

`README.md` around line 416 still says, under **Roadmap**:

> **A Blazor client, on Fluent UI v5** … `src/client/web/` is currently an empty solution folder reserved
> for it … Neither has landed yet.

That was already wrong before this work started — commit `e3f0c09` added the client but did not update
the README, despite the commit message claiming it did. Move the entry out of Roadmap and describe what
actually exists: the three-project split, the BFF, `InteractiveAuto`, and the bUnit suite.

## 6. Optional — a client architecture test

Not required, and deliberately **not** an addition to the server's suite.
`tests/server/…Tests.Architecture` enumerates its `ProjectReference`s by hand and
`ConfigurationTests` names eight assemblies; adding a client project there would immediately fire
`No_Constructor_Should_Inject_IConfiguration` on the `OidcOptions` binding, and `HandlerTests` /
`NoBusinessBranchingTests` have no meaningful reading for Razor components.

If client rules are wanted, they belong in a separate `tests/client/Tnosc.EShop.Client.Tests.Architecture`
with its own ruleset. Rules worth encoding:

- Nothing under `Features/**` references `HttpClient` directly — all API access goes through an `I*Api`.
- No `ApiRoutes` constant starts with `/`.
- Every component under `Features/Admin/**` carries an `[Authorize]` attribute (the one the compiler
  cannot enforce).

That last one is the highest-value of the three, because it mechanises the gotcha that `_Imports.razor`
cannot solve.

---

## Definition of done

- [ ] `/products` issues **one** `GET /api/catalog/products` per page load, not two.
- [ ] `dotnet publish -c Release` on `.Client` succeeds with trimming on, and prices still format
      correctly in the published app.
- [ ] `src/client/CLAUDE.md` exists and covers the six points above.
- [ ] ADR-017 exists in `docs/decisions/`.
- [ ] The README no longer claims `src/client/web/` is an empty folder.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean and every suite is green:

```bash
dotnet build Tnosc.EShop.slnx
dotnet test  tests/client/Tnosc.EShop.Client.Tests.Unit
dotnet test  tests/server/Tnosc.EShop.Server.Tests.Unit
dotnet test  tests/server/Tnosc.EShop.Server.Tests.Architecture
dotnet test  tests/server/Tnosc.EShop.Server.Tests.Integration   # Docker
```
