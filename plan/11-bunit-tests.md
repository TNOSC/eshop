# Task 11 — bUnit tests

**Goal:** a client test suite that catches the things most likely to break silently — the error contract,
the validation bridge, and the idempotency key.

**Depends on:** [09](09-admin-catalog.md). Can be done alongside [10](10-skeletons.md) and
[12](12-polish-and-docs.md).

---

## Project

`tests/client/Tnosc.EShop.Client.Tests.Unit/` — matching the existing `tests/server/Tnosc.EShop.Server.Tests.*`
naming, under a new `/tests/client/` folder in `Tnosc.EShop.slnx` (the file already has an empty `/tests/`
folder).

`tests/Directory.Build.props` applies automatically: `GenerateDocumentationFile=false`, `IsPackable=false`,
and analyzers + warnings-as-errors still on.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Shouldly" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Bogus" />
    <!-- bUnit v2 is test-framework agnostic: it carries no xunit dependency, so it composes with
         this repo's xunit 2.9.3 rather than forcing xunit.v3. -->
    <PackageReference Include="bunit" />
  </ItemGroup>

  <ItemGroup>
    <!-- Every renderable component lives in .Client; Contracts comes along transitively. -->
    <ProjectReference Include="..\..\..\src\client\Tnosc.EShop.Client.Web\Tnosc.EShop.Client.Web.Client\Tnosc.EShop.Client.Web.Client.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

`Directory.Packages.props`: `<PackageVersion Include="bunit" Version="2.9.0" />`.

bUnit 2.9.0 ships a native `net10.0` dependency group built against `Microsoft.AspNetCore.Components`
10.0.10 — the exact version this solution uses. No compatibility shim is needed.

---

## bUnit v2, not v1

**The base class is `BunitContext`, not `TestContext`.** v2 renamed it to stop it clashing with the
MSTest and NUnit types of the same name, and kept `TestContext` only as a **deprecated** shim — which
under this repo's `TreatWarningsAsErrors` **will not compile at all**. Any v1 sample you copy needs
adjusting.

Two related v2 changes:

- The `RenderComponent` / `Render` / `SetParametersAndRender` overloads are unified into a single
  **`Render`**.
- `BunitContext` implements `IAsyncDisposable` as well as `IDisposable`, so fixtures must dispose
  asynchronously.

---

## The shared context

```csharp
public abstract class EShopComponentTestContext : BunitContext
{
    protected ICatalogApi CatalogApi { get; } = Substitute.For<ICatalogApi>();
    protected IDialogService DialogService { get; } = Substitute.For<IDialogService>();
    protected INotificationService Notifications { get; } = Substitute.For<INotificationService>();

    protected EShopComponentTestContext()
    {
        // Fluent UI v5 components import JS modules on first render; strict mode throws
        // JSRuntimeUnhandledInvocationException on the very first import.
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddFluentUIComponents();
        Services.AddSingleton(implementationInstance: CatalogApi);
        Services.AddSingleton(implementationInstance: DialogService);
        Services.AddSingleton(implementationInstance: Notifications);

        SetRendererInfo(rendererInfo: new RendererInfo(rendererName: "Server", isInteractive: true));
    }
}
```

Three things that are not optional:

1. **`AddFluentUIComponents()` is mandatory.** Every v5 component takes `LibraryConfiguration` through its
   **constructor**, so without the registration the *renderer* throws on activation — before any interop
   happens. The error will not mention Fluent UI.
2. **`JSRuntimeMode.Loose` is mandatory.** `FluentNav` calls into JS in `OnAfterRenderAsync`,
   `FluentDialogProvider` drives `HTMLDialogElement.showModal`, and `FluentJSModule` imports from
   `_content/…`. Per-module `JSInterop.SetupModule(...)` is possible but brittle across rc bumps and not
   worth it.
3. **`SetRendererInfo`** so components that branch on `RendererInfo.IsInteractive` take the interactive
   path rather than the prerender one.

### Do not render `<FluentProviders />` in unit tests

Substitute `IDialogService` and `INotificationService` and assert the **call**:

```csharp
await DialogService.Received(requiredNumberOfCalls: 1)
    .ShowDialogAsync<CreateProductDialog>(options: Arg.Any<DialogOptions>());
```

Rendering the providers would pull in three overlay containers, a JS module each, and a real
`DialogService` that expects a live provider. Test a dialog component **directly** instead, passing a
substituted `IDialogInstance` as a cascading parameter.

### Assert on our own markup, never Fluent internals

`<fluent-data-grid>` is a web component — bUnit does not render its shadow DOM, and its internals are not
a contract. Give components stable `data-testid` attributes and assert on those. A test that asserts on
Fluent's generated markup breaks on every rc bump for no signal.

### Auth

`AddAuthorization().SetAuthorized("admin@eshop.local").SetRoles("admin")` from `Bunit.TestDoubles`.

---

## Conventions

From [`tests/CLAUDE.md`](../tests/CLAUDE.md):

- `MethodOrScenario_Should_ExpectedOutcome_When_Condition` (`CA1707` is suppressed, underscores are fine).
- Test classes are `public sealed`.
- **Shouldly, not `Assert`**, with named arguments: `result.IsSuccess.ShouldBeTrue()`,
  `problem.Title.ShouldBe(expected: "Product.NotFound")`.
- **NSubstitute** over the client interfaces — `Substitute.For<ICatalogApi>()` — mirroring how the server
  suite substitutes `IProductRepository`.
- **Bogus** for data, via a local `ProductSummaryFaker`.
- `// Arrange` / `// Act` / `// Assert` comment blocks.

---

## Layout

```
tests/client/Tnosc.EShop.Client.Tests.Unit/
├─ Infrastructure/  EShopComponentTestContext.cs   ProductSummaryFaker.cs
├─ Api/             ApiResponseReaderTests.cs      ApiProblemTests.cs
├─ Errors/          ValidationCodeFieldMapTests.cs
├─ Features/Store/Catalog/   ProductsTests.cs   ProductDetailTests.cs
├─ Features/Admin/Catalog/   AdminProductsTests.cs   CreateProductDialogTests.cs
└─ Layout/Admin/    AdminNavTests.cs
```

---

## The tests, most valuable first

| Test | Proves |
|---|---|
| `ApiResponseReaderTests` | 204 → `Success(default)`; a bare-`Guid` 201 body deserializes; a `Result` 400 → `Errors` keyed by **code**; a 500 → `ErrorCode` + `TraceId`; a **non-JSON body does not throw** |
| `ValidationCodeFieldMapTests` | Every mapped code resolves to a real property name; no duplicates. A tripwire for server-vocabulary drift |
| `CreateProductDialogTests` | **The same idempotency key is used across two submits without an intervening response.** The only test that catches a duplicate-order bug before production |
| `CreateProductDialogTests` | A code-keyed 400 lands **inline on the Sku field**; an **unmapped** code reaches the message bar rather than vanishing |
| `CreateProductDialogTests` | An invalid form does **not** call `ICatalogApi` at all |
| `ProductsTests` | One card per returned item; the empty state on zero results; typing in search re-queries with `Page = 1` (not the page the user was on) |
| `AdminProductsTests` | Grid rows come from the substituted `ICatalogApi`; clicking "Price" opens `UpdateProductPriceDialog` |
| `AdminNavTests` | The `admin` role sees `/admin` links; anonymous does not |

**The first two need no bUnit at all** — plain xunit + Shouldly against pure functions. They are the
highest value per line in the suite, and they are the two that keep working when Fluent UI's rc version
moves. Write them first.

---

## Definition of done

- [ ] The project exists, is in `.slnx`, and uses `BunitContext` (no `TestContext` anywhere).
- [ ] `dotnet test tests/client/Tnosc.EShop.Client.Tests.Unit` is green.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean — warnings are errors in the test project too.
- [ ] `dotnet test tests/server/Tnosc.EShop.Server.Tests.Architecture` is **still** green, and no client
      `ProjectReference` was added to it (see [`00-overview.md`](00-overview.md)).
- [ ] No test asserts on Fluent UI's own generated markup.
