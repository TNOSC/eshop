/# Task 01 — Guardrails

**Goal:** make the Razor toolchain analyzer-clean and compilable *before* any feature code exists, and
clear the template debris out of the way.

**Depends on:** nothing. This is the first task.

**Why first:** the template compiles today only by accident. `Counter.razor` uses no `System` type, so
nobody has noticed that neither `_Imports.razor` imports one. The first `async Task` in an `@code` block
fails with `CS0246: The type or namespace name 'Task' could not be found` — `ImplicitUsings` is
**disabled** repo-wide in `Directory.Build.props`. Finding that out halfway through writing a page is a
waste; finding it out now costs five minutes.

---

## Files to edit

| File | Change |
|---|---|
| `src/client/Tnosc.EShop.Client.Web/Tnosc.EShop.Client.Web/Components/_Imports.razor` | add `System*` usings |
| `src/client/Tnosc.EShop.Client.Web/Tnosc.EShop.Client.Web.Client/_Imports.razor` | add `System*` usings |
| `.editorconfig` | add a `[*.razor]` section |
| `src/client/…/Tnosc.EShop.Client.Web/Components/App.razor` | add `default-fuib.css` |
| `src/client/…/Tnosc.EShop.Client.Web/wwwroot/app.css` | strip template CSS |

## Files to delete

| File | Why |
|---|---|
| `…/Tnosc.EShop.Client.Web/Components/Pages/Home.razor` | replaced in task 06 |
| `…/Tnosc.EShop.Client.Web/Components/Pages/Weather.razor` | template demo; also the only `#pragma warning disable CA5394` in the client |
| `…/Tnosc.EShop.Client.Web/Components/Layout/MainLayout.razor` (+ `.css`) | replaced by `StoreLayout` in task 03 |
| `…/Tnosc.EShop.Client.Web/Components/Layout/NavMenu.razor` (+ `.css`) | stock `NavLink`/`bi-*` markup, replaced by `FluentNav` |
| `…/Tnosc.EShop.Client.Web.Client/Pages/Counter.razor` | template demo |

**Keep** `Components/Pages/Error.razor`, `Components/Pages/NotFound.razor` (static SSR error pages, still
wired to `UseStatusCodePagesWithReExecute` and `NotFoundPage=`) and `Components/Layout/ReconnectModal.*`
(the Blazor Server reconnect UI — still needed under `InteractiveAuto`).

> Deleting `MainLayout.razor` breaks `Routes.razor`, which references `DefaultLayout="typeof(Layout.MainLayout)"`.
> Task 03 replaces that line. To keep this task's build green on its own, either do 01 and 03 back to
> back, or leave `MainLayout.razor` in place until 03 and delete it there. **Recommended: leave
> `MainLayout`/`NavMenu` for task 03** and delete only `Home`, `Weather` and `Counter` here.

---

## Steps

### 1. `System*` usings in both `_Imports.razor`

Add to **both** files:

```razor
@using System
@using System.Collections.Generic
@using System.Globalization
@using System.Linq
@using System.Threading
@using System.Threading.Tasks
```

`System.Globalization` earns its place immediately — see the `CA1305` note below.

Any `.razor.cs` code-behind is a plain `.cs` file and needs every `using` written out as usual; these
`@using` directives do not reach it.

### 2. A `[*.razor]` section in `.editorconfig`

`.editorconfig` today has exactly two sections: `[*.cs]` and `[*.{cs,vb}]`. **Neither matches `.razor`.**

Razor compiles through `obj/**/*.razor.g.cs`, so the ~60 existing suppressions apply to `@code` blocks
only by path coincidence — the generated file happens to end in `.cs`. That is fragile, and it does not
cover diagnostics the Razor compiler reports against the `.razor` file itself.

Add a `[*.razor]` section mirroring the `[*.cs]` `dotnet_diagnostic` block, in the same grouped,
commented style as the existing entries. Per
[`.claude/rules/analyzer-suppressions.md`](../.claude/rules/analyzer-suppressions.md), say so in the
commit message.

**Rules likely to fire in new client code that are *not* currently suppressed:**

| Rule | Where it bites | Fix — not a suppression |
|---|---|---|
| `CA1305` / `CA1310` / `MA0011` | culture-less `ToString`/`Format` on prices | always pass `CultureInfo.InvariantCulture` explicitly |
| `CA1054` | URI-ish parameter typed as `string` — hits `ApiRoutes` and the typed clients | take a `Uri`, or suppress narrowly with the reason inline |
| `MA0048` | file name must match type — matters for `.razor.cs` partials | name the partial after the component |
| `CA1848` | `LoggerMessage` delegates | use the source-generated logging pattern |

`CA1031`, `CA1034`, `CA2007`, `CA1707` and `MA0004` are **already off** and must stay off.

### 3. Fluent UI stylesheet in `App.razor`

`App.razor` already has `<ResourcePreloader />`, `<ImportMap />`, the `reboot.css` link and the manual
`lib.module.js` script tag (with a comment explaining static SSR needs it). Add the main stylesheet:

```html
<link rel="stylesheet"
      href="_content/Microsoft.FluentUI.AspNetCore.Components/css/default-fuib.css" />
```

Leave the existing `reboot.css` link and the `lib.module.js` script exactly as they are.

### 4. Strip the template CSS

`wwwroot/app.css` and `Components/Layout/MainLayout.razor.css` are stock Bootstrap-ish rules — a
blue/purple sidebar gradient that fights every Fluent token. Reduce `app.css` to what is still needed
(the `#blazor-error-ui` block, `.loading-progress`) and drop the rest. Task 03 rebuilds layout styling on
Fluent custom properties (`var(--colorNeutralBackground1)`, `var(--fontSizeBase300)`, …).

---

## Definition of done

- [ ] Both `_Imports.razor` files carry the six `System*` usings.
- [ ] `.editorconfig` has a `[*.razor]` section mirroring `[*.cs]`.
- [ ] `default-fuib.css` is linked in `App.razor`.
- [ ] `Home.razor`, `Weather.razor` and `Counter.razor` are gone.
- [ ] `app.css` no longer contains template gradients.
- [ ] `dotnet build Tnosc.EShop.slnx` is **clean**.

**The real proof:** drop a throwaway page with `@code { private async Task NoOpAsync() => await Task.Delay(1); }`
into `.Client`, confirm it compiles, then delete it. If that fails, step 1 was not applied to the right
`_Imports.razor`.
