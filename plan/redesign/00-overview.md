# Storefront Redesign — Overview

Porting the visual language of `C:\Projects\eShop\src\WebApp` onto `src/client/Tnosc.EShop.Client.Web/`,
which runs Fluent UI v5 rc.5. Every Fluent **control** stays; what changes is the shell, the layout
geometry, and the introduction of a design-token layer that does not exist today.

This series is **independent of and later than** [`../00-overview.md`](../00-overview.md) (tasks 00-12),
which built the client and is complete. Nothing here reopens those decisions — the BFF, the two-layout
mechanism, the contracts project and the API clients are all untouched.

## Why

`wwwroot/app.css` is 20 lines containing only the `#blazor-error-ui` block. There is no `:root`, no
token layer, no breakpoint of our own, and no brand asset beyond `favicon.png`. The app is raw Fluent
defaults inside a generic header plus a 250px nav rail.

The reference is a deliberately flat, high-contrast, **editorial monochrome** storefront: a full-bleed
photo hero with the logo and icons floating on it and the page title set into it; no nav rail; square
corners; exactly two shadows in the whole app; image-first product cards that reveal a 2px border on
hover; pill-shaped filter tags; outlined order-status chips.

The reference achieves this with ~1,500 lines of hand-written CSS, **zero** custom properties and **no**
Fluent UI. We are not copying its CSS — we are re-expressing its decisions as tokens over Fluent's.

## Task order

Tasks **01 → 03 are strictly sequential** — everything downstream consumes the tokens and the shell.
Tasks **04 → 09 are independent of each other** once 03 is green. Task 10 is the closing gate.

| # | Task | Ends when |
|---|---|---|
| [01](01-design-tokens.md) | Design tokens | `app.css` carries the token layer; the toggle still flips theme |
| [02](02-brand-assets.md) | Brand assets | Logo, hero, icons and product images resolve at their URLs |
| [03](03-store-shell.md) | Store shell | Hero + floating nav bar + footer render; the rail is gone; the badge updates live |
| [04](04-catalog-grid.md) | Catalogue grid | 3-up image cards, pill filters, chip pagination |
| [05](05-product-detail.md) | Product detail | Two-column item page with the price/button row |
| [06](06-basket-and-checkout.md) | Basket and checkout | Flex pseudo-table + summary panel; sectioned form |
| [07](07-orders.md) | Orders | Four-column list with outlined status pills |
| [08](08-admin-console.md) | Admin console | Rail retained, tokens adopted, dialogs actually open |
| [09](09-error-pages.md) | Error pages | No template markup left anywhere |
| [10](10-verification.md) | Verification | The full gate passes |

**Every task ends on a green `dotnet build Tnosc.EShop.slnx`.** Warnings are errors here.

---

## The constraint that shapes everything

`Components/App.razor` renders `<Routes />` **with no render mode**, deliberately — the comment on
lines 22-25 explains that `Routes.razor` lives in the server project and is never shipped to the WASM
bundle, so an interactive mode on it fails at runtime once WebAssembly is chosen.

Therefore `Router` → `AuthorizeRouteView` → **the layout all render under static SSR.** Only the routed
page is an interactive island. Two consequences:

### 1. `SectionOutlet` / `SectionContent` cannot carry the hero title

The reference's entire title mechanism is `<SectionOutlet SectionName="page-header-title" />` in
`HeaderBar` fed by `<SectionContent …>` in each page. Here the outlet would sit in the statically
rendered layout and the content in an interactive page — opposite sides of a render mode boundary,
which sections do not cross. It would silently render an empty `<h1>`.

**Instead:** the layout's nav bar is `position: absolute; z-index: 2` with a transparent background, and
each page renders `<StoreHero Title=… Subtitle=… Tall=… />` as its first element with `padding-top`
clearing the bar. Separate DOM, identical visual result, dynamic titles (product name, order number)
still work, no boundary crossed.

The reference also derives "am I the home page?" from `HttpContext` endpoint metadata. We do not need
that either — `Tall="true"` is a parameter the Home page passes.

### 2. Anything interactive in the header needs its own island

A static parent hosting an interactive child is the supported direction:

```razor
<BasketBadge @rendermode="InteractiveAuto" />
```

Components declared this way must take no non-serializable parameters. `BasketBadge`, `ThemeToggle`
and `LoginDisplay` take none, so all three qualify as-is.

---

## Four live defects fixed by this series

Found while tracing the render-mode boundary. Each is fixed in the task that touches its file, not as a
separate change.

| Defect | Where | Effect today | Fixed in |
|---|---|---|---|
| Basket badge never updates | `Layout/Store/BasketBadge.razor.cs` subscribes to `BasketState.Changed` from the **static SSR** layout | The scoped `BasketState` there is a different instance from the interactive page's. Adding to basket does not move the header count until a full reload. | [03](03-store-shell.md) |
| Theme toggle inert | `Layout/Store/ThemeToggle.razor` calls `IThemeService.SwitchThemeAsync()` from an `OnClick` in the static layout | The click never dispatches. | [03](03-store-shell.md) |
| Dialogs unprovided | `Layout/Admin/AdminLayout.razor` has `FluentProviders` + `FluentToastProvider` but **no `FluentDialogProvider`** | `AdminProducts` opens three dialogs against a missing provider — fails silently, no exception. | [08](08-admin-console.md) |
| `/orders` unreachable | `Layout/Store/StoreNav.razor` lists Home, Catalogue, Basket only | The page exists and works; nothing links to it. | [03](03-store-shell.md) |

---

## The token layer

One `:root` block in `src/client/Tnosc.EShop.Client.Web/Tnosc.EShop.Client.Web/wwwroot/app.css`. Every
value routes through a Fluent neutral token, so the existing `IThemeService` toggle keeps working and
one ruleset serves both themes.

**No hard-coded colour may appear in any component CSS.** Task [10](10-verification.md) greps for it.

| Token | Fluent source | Reference literal | Role |
|---|---|---|---|
| `--eshop-ink` | `--colorNeutralForeground1` | `#000` | Ink, primary button background, active pill |
| `--eshop-on-ink` | `--colorNeutralBackground1` | `#FFF` | Text on ink |
| `--eshop-surface` | `--colorNeutralBackground1` | `#FFF` | Page surface |
| `--eshop-muted` | `--colorNeutralForeground3` | `#444` | Price, form labels, secondary text |
| `--eshop-rule` | `--colorNeutralStroke2` | `#D2D2D2` | Row dividers, section underlines |
| `--eshop-panel` | `--colorNeutralBackground2` | `#F7F7F7` | Cart summary, pagination chips |
| `--eshop-hover` | `--colorNeutralBackground3` | `#ddd` | Pill and dropdown hover |
| `--eshop-status-neutral` | `--colorNeutralForeground4` | `#A3A3A3` | Pending order pill |
| `--eshop-status-good` | `--colorStatusSuccessForeground1` | `#2A9E01` | Paid / confirmed |
| `--eshop-status-bad` | `--colorStatusDangerForeground1` | `#FF4E4E` | Cancelled |

**Typography** is the reference's scale on Fluent's family (`var(--fontFamilyBase)`) — no woff2 files
are shipped. `3.5rem/700` hero h1 (100% line-height) · `2rem/700` hero subtitle (125%) · `1.6rem/600`
item price · `1.25rem/600` section h2 (140%) · `1rem/600` card name and price (150%) · `1rem/400` body
and filter pills · `0.75rem/400` status pills and badges.

**Geometry.** Page max width `120rem`. Gutter `10rem` desktop → `3rem` at 481-1024px → `1rem` at
≤480px, carried by a single `.eshop-page` class rather than repeated in six page CSS files as the
reference does. Two-column gap `6rem`; grid gap `2.5rem`. Radii only on pills (`1.25rem`) and badges
(`0.75rem`) — **cards and images are square**. Two shadows total. Breakpoints are `max-width: 480px`
and `min-width: 481px and max-width: 1024px`, mobile-last.

### Not ported

- The `.cart-badge` whose `position: absolute` resolves against `.eshop-header-container` instead of its
  anchor, pinning the count to the wrong corner.
- The unscoped `::deep a` in `Catalog.razor.css`, which leaks styling to every descendant anchor.
- The dead `.catalog-filter-*` and `.cart-summary-breakdown` rulesets (no matching markup).
- The missing generic font fallback (`font-family: 'Plus Jakarta Sans';` with nothing after it).

---

## Conventions

Everything in [`../../.claude/rules/code-style.md`](../../.claude/rules/code-style.md) applies to `.cs`
files here, and [`../00-overview.md`](../00-overview.md) already restated the client-specific parts:

- File header, explicit `using`s (System first), file-scoped namespace, one public type per file.
- **Named arguments at every call site**; 3+ parameters ⇒ one per line.
- Explicit types, not `var`, except where apparent.
- Every method returning `Task`/`ValueTask` is named `…Async`.
- **Razor components take dependencies via `[Inject]` properties** — the sanctioned exception to the
  primary-constructor rule, because the framework requires a parameterless component.
- **Code-behind `.razor.cs` partial classes for anything past trivial markup.**

CSS: prefer a **scoped `.razor.css`** next to the component. Only genuinely global things — the token
`:root`, `.eshop-page`, `@font-face`-free resets — belong in `app.css`. Scoped files are bundled into
`Tnosc.EShop.Client.Web.styles.css`, which `App.razor` already links.

> Scoped CSS and `::deep`: a scoped file only matches elements the component itself renders. To reach
> into a Fluent component's rendered children, use `::deep`, and **always anchor it to a class of
> ours** (`.eshop-card ::deep a`) — never bare `::deep a`, which is the reference's leak.

## Fluent UI v5 rc.5 — the skill file is wrong in five places

Restated from [`../00-overview.md`](../00-overview.md) because it is the single biggest risk in a
markup-heavy change. An unknown component name yields **`RZ10012`, a warning that does not fail this
build** — it renders a broken unknown HTML element instead.

| The skill says | rc.5 actually has |
|---|---|
| `FluentTextField` | **`FluentTextInput`** |
| `FluentNumberField` | **`FluentNumberInput<TValue>`** |
| "`IToastService` removed" | **`INotificationService` exists** |
| `FluentNavItem Icon="…"` | **`IconRest`** / **`IconActive`** |
| `FluentNavCategory Label="…"` | **`Title`** |

There is no `FluentSearch` (use `FluentTextInput TextInputType="TextInputType.Search"`) and no
`FluentDesignTheme`.

## Packages

**None are added by this series.** No `Directory.Packages.props` change. Central Package Management is
on, so if that ever stops being true, add a `<PackageVersion>` and reference the package bare.

## Verification

The full gate lives in [10](10-verification.md). Every intermediate task ends on:

```bash
dotnet build Tnosc.EShop.slnx
```
