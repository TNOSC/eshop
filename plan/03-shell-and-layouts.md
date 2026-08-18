# Task 03 — Shell and layouts

**Goal:** the two-layout skeleton on Fluent UI v5 — `StoreLayout`, `AdminLayout`, `FluentNav`, theme
toggle, and a `Routes.razor` that authorizes. Placeholder pages only; no API calls yet.

**Depends on:** [02](02-contracts-project.md).

---

## Files to create — all in `.Client`

```
Tnosc.EShop.Client.Web.Client/
├─ Layout/
│  ├─ Store/  StoreLayout.razor   StoreHeader.razor   StoreNav.razor
│  │          ThemeToggle.razor   LoginDisplay.razor  BasketBadge.razor
│  └─ Admin/  AdminLayout.razor   AdminNav.razor
├─ Features/
│  ├─ Shared/ PageHeader.razor  LoadingPanel.razor  ErrorPanel.razor
│  │          MoneyDisplay.razor  RedirectToLogin.razor
│  ├─ Store/  Home.razor  Catalog/Products.razor          (placeholders)
│  └─ Admin/
│     ├─ _Imports.razor                                   ← the whole two-layout mechanism
│     └─ AdminDashboard.razor                             (placeholder)
```

## Files to edit

| File | Change |
|---|---|
| `…/Tnosc.EShop.Client.Web/Components/Routes.razor` | `AuthorizeRouteView`, `DefaultLayout="typeof(StoreLayout)"` |
| both `Program.cs` | `AddCascadingAuthenticationState()`, `AddAuthorizationCore()` / `AddAuthorization()` |
| `…/Tnosc.EShop.Client.Web/Components/App.razor` | global `@rendermode InteractiveAuto` |

## Files to delete (deferred from task 01)

`Components/Layout/MainLayout.razor` (+ `.css`) and `Components/Layout/NavMenu.razor` (+ `.css`).
Keep `ReconnectModal.*` — the Blazor Server reconnect UI is still live under `InteractiveAuto`.

---

## The two-layout mechanism

**One file does it.** `Features/Admin/_Imports.razor`:

```razor
@layout Tnosc.EShop.Client.Web.Client.Layout.Admin.AdminLayout
```

That applies `AdminLayout` to every page in `Features/Admin/` and its subfolders. `StoreLayout` becomes
`DefaultLayout` on `AuthorizeRouteView`, so everything else gets it. No route-prefix sniffing, no
per-page `@layout`.

> ⚠️ **`@attribute [Authorize]` is NOT inherited through `_Imports.razor`.** Only `@using`, `@layout`,
> `@inject` and `@namespace` propagate. Every admin page needs its own
> `@attribute [Authorize(Roles = "admin")]` written out. There is no way around this — put it in the
> checklist for every new admin page.

---

## `StoreLayout.razor`

```razor
@inherits LayoutComponentBase

<FluentLayout HeaderSticky="true">
    <FluentLayoutItem Area="LayoutArea.Header">
        <FluentStack Orientation="Orientation.Horizontal" VerticalAlignment="VerticalAlignment.Center">
            <FluentLayoutHamburger />
            <FluentAnchorButton Href="/" Appearance="ButtonAppearance.Transparent">Tnosc EShop</FluentAnchorButton>
            <FluentSpacer />
            <BasketBadge />
            <ThemeToggle />
            <LoginDisplay />
        </FluentStack>
    </FluentLayoutItem>

    <FluentLayoutItem Area="LayoutArea.Menu">
        <FluentNav Density="NavDensity.Comfortable">
            <FluentNavItem Href="/" Match="NavLinkMatch.All"
                           IconRest="@(new Icons.Regular.Size20.Home())">Home</FluentNavItem>
            <FluentNavItem Href="/products"
                           IconRest="@(new Icons.Regular.Size20.Box())">Catalogue</FluentNavItem>
            <FluentNavItem Href="/basket"
                           IconRest="@(new Icons.Regular.Size20.Cart())">Basket</FluentNavItem>
            <AuthorizeView Roles="admin" Context="adminContext">
                <FluentNavSectionHeader Title="Administration" />
                <FluentNavItem Href="/admin"
                               IconRest="@(new Icons.Regular.Size20.Settings())">Back office</FluentNavItem>
            </AuthorizeView>
        </FluentNav>
    </FluentLayoutItem>

    <FluentLayoutItem Area="LayoutArea.Content">
        @Body
    </FluentLayoutItem>
</FluentLayout>

<div id="blazor-error-ui" data-nosnippet>…</div>
<FluentProviders />
```

`AdminLayout.razor` is the same skeleton with `AdminNav` — `FluentNavCategory Title="Catalog"` → Products,
`Title="Identity"` → Customers — plus a "back to store" anchor.

**Note the v5 parameter names**, which the skill file gets wrong: `IconRest` (not `Icon`) on
`FluentNavItem`, `Title` (not `Label`) on `FluentNavCategory` and `FluentNavSectionHeader`.

### `<FluentProviders />` goes in **both** layouts

Exactly once per rendered page. It supplies `FluentDialogProvider`, `FluentToastProvider`,
`FluentMessageBarProvider`, `FluentTooltipProvider` and `FluentKeyCodeProvider`. Without it, dialogs and
toasts fail **silently** — no exception, nothing appears.

---

## Theming

v5 has no `FluentDesignTheme`. Theming is CSS custom properties driven from JS. Prefer the typed service
over raw interop:

```razor
@inject IThemeService ThemeService

<FluentButton IconStart="@(new Icons.Regular.Size20.WeatherMoon())"
              Appearance="ButtonAppearance.Transparent"
              OnClick="@ToggleThemeAsync" />

@code {
    private async Task ToggleThemeAsync() => await ThemeService.SwitchThemeAsync();
}
```

Rebuild layout styling on Fluent tokens — `var(--colorNeutralBackground1)`,
`var(--colorNeutralForeground1)`, `var(--fontSizeBase300)`, `var(--borderRadiusMedium)` — never on
hard-coded colours, or dark mode breaks.

---

## `Routes.razor`

```razor
<Router AppAssembly="typeof(Program).Assembly"
        AdditionalAssemblies="new[] { typeof(Client._Imports).Assembly }"
        NotFoundPage="typeof(Pages.NotFound)">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(StoreLayout)">
            <NotAuthorized><RedirectToLogin /></NotAuthorized>
            <Authorizing><FluentProgressRing /></Authorizing>
        </AuthorizeRouteView>
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

No `<CascadingAuthenticationState>` wrapper — `AddCascadingAuthenticationState()` in DI supplies it in
both hosts.

`RedirectToLogin.razor` lives in `Features/Shared/`:

```csharp
Navigation.NavigateTo(
    uri: $"bff/login?returnUrl={Uri.EscapeDataString(stringToEscape: Navigation.Uri)}",
    forceLoad: true);
return;
```

Two non-negotiable details:

- **`forceLoad: true` is mandatory.** Without it, Blazor's enhanced navigation intercepts the request and
  the OIDC challenge never reaches the server — you get a silent no-op instead of a login page.
- **`return;` immediately after.** Both web csprojs set `BlazorDisableThrowNavigationException=true`, so
  `NavigateTo` during static SSR **returns normally** instead of throwing. Any code after it keeps
  running.

The endpoint it points at does not exist until task 07; that is fine, nothing is authorized yet.

---

## Render mode

Apply `InteractiveAuto` globally in `App.razor`:

```razor
<HeadOutlet @rendermode="InteractiveAuto" />
...
<Routes @rendermode="InteractiveAuto" />
```

Pages that must not prerender get `@rendermode @(new InteractiveAutoRenderMode(prerender: false))`
individually — every authenticated page, from task 06 onward. That kills the double-fetch *and* the
"prerender shows the anonymous state, then it flips to authenticated" flash.

---

## DI

`.Client/Program.cs`:

```csharp
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
```

Host `Program.cs`:

```csharp
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
```

`AddFluentUIComponents()` is already called in both — leave it.

---

## The grep gate

**This task does not end until this returns zero hits.** An unknown Fluent component name yields
`RZ10012`, which is a *warning* that does not fail the build — it renders a broken unknown HTML element
instead. It is the most likely silent failure in the project.

```bash
grep -rnE 'FluentTextField|FluentNumberField|IToastService|FluentNavMenu|FluentNavLink|FluentDesignTheme|Appearance\.Accent|SelectedOptions|IDialogContentComponent' src/client --include=*.razor --include=*.cs
```

If it hits, consult the correction table in [`00-overview.md`](00-overview.md), not the skill file.

---

## Definition of done

- [ ] `StoreLayout` and `AdminLayout` render, each ending in `<FluentProviders />`.
- [ ] `Features/Admin/_Imports.razor` carries `@layout AdminLayout`, and `/admin` visibly uses it.
- [ ] `Routes.razor` uses `AuthorizeRouteView` with `DefaultLayout="typeof(StoreLayout)"`.
- [ ] The theme toggle flips light/dark and the layout follows (proving Fluent tokens, not hard-coded colours).
- [ ] Stock `MainLayout` and `NavMenu` are deleted.
- [ ] **The grep gate returns zero hits.**
- [ ] `dotnet build Tnosc.EShop.slnx` is clean, and `dotnet run --project aspire/Tnosc.EShop.AppHost`
      renders both shells.
