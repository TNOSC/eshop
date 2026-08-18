# Task 03 — Store shell

**Goal:** replace the Fluent header + 250px nav rail with the reference's shell — a full-bleed hero band
with the nav bar floating over it and a footer band — and give the header's interactive parts their own
render-mode islands so they actually work.

**Depends on:** [01](01-design-tokens.md), [02](02-brand-assets.md).

This is the structural task. Everything from [04](04-catalog-grid.md) onward assumes this shape.

---

## Files to create — all in `.Client/Layout/Store/`

```
StoreFooter.razor  (+ .razor.css)
StoreHero.razor    (+ .razor.css)
UserMenu.razor     (+ .razor.css)
StoreHeader.razor.css
StoreLayout.razor.css
```

## Files to edit

| File | Change |
|---|---|
| `Layout/Store/StoreLayout.razor` | `FluentLayout` → semantic shell: floating `<header>`, `@Body`, `<StoreFooter />` |
| `Layout/Store/StoreHeader.razor` | Inline logo, inline nav links, `UserMenu`, islands for the three interactive parts |
| `Layout/Store/BasketBadge.razor` | Re-render on the icon + count markup; keep the `.razor.cs` as-is |
| `Layout/Store/ThemeToggle.razor` | Icon button on tokens |
| `Layout/Store/LoginDisplay.razor` | Folded into `UserMenu` (see below) |
| every store page | Add `<StoreHero …>` as the first element, wrap the rest in `.eshop-page` |

## Files to delete

`Layout/Store/StoreNav.razor` — the rail is gone. Its four links move into the header bar, **plus the
missing `/orders` link** (defect 4).

---

## The shell

`StoreLayout.razor` drops `FluentLayout` **for the store only**. A full-bleed hero and a footer band
fight its `LayoutArea` grid, and with the rail gone there is nothing left that `FluentLayout` was
providing. Fluent components remain everywhere for controls — this is a container swap, not a
de-Fluent-ing. `AdminLayout` keeps `FluentLayout` (see [08](08-admin-console.md)).

```razor
@namespace Tnosc.EShop.Client.Web.Client.Layout.Store
@inherits LayoutComponentBase

<div class="eshop-shell">
    <StoreHeader />
    <main class="eshop-main">
        @Body
    </main>
    <StoreFooter />
</div>

<div id="blazor-error-ui" data-nosnippet>
    An unhandled error has occurred.
    <a href="." class="reload">Reload</a>
    <span class="dismiss">🗙</span>
</div>

<FluentProviders />
<FluentToastProvider />
<FluentDialogProvider />
<FluentTooltipProvider />
```

Keep all four providers exactly as they are today. They render under static SSR, which is why
`App.razor` manually includes the Fluent `lib.module.js` — do not remove that script tag.

`StoreLayout.razor.css`:

```css
.eshop-shell {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
    position: relative;
}

.eshop-main {
    flex: 1 0 auto;
}
```

`min-height: 100vh` + `flex: 1 0 auto` is what keeps the footer at the bottom on a short page. The
reference has no equivalent and its footer floats up on the empty-basket page.

## The floating nav bar

The bar sits **over** the hero: `position: absolute; top: 0; z-index: 2`, transparent background. The
hero supplies its own top padding so the title clears the bar.

```razor
@* StoreHeader.razor *@
@namespace Tnosc.EShop.Client.Web.Client.Layout.Store

<header class="eshop-navbar">
    <div class="eshop-navbar-inner">
        <a class="eshop-logo" href="/" aria-label="Tnosc EShop — home">
            @* inline SVG wordmark, fill="currentColor" *@
        </a>

        <nav class="eshop-navlinks">
            <NavLink href="/" Match="NavLinkMatch.All" ActiveClass="active">Home</NavLink>
            <NavLink href="/products" ActiveClass="active">Catalogue</NavLink>
            <AuthorizeView>
                <Authorized>
                    <NavLink href="/orders" ActiveClass="active">Orders</NavLink>
                </Authorized>
            </AuthorizeView>
        </nav>

        <div class="eshop-navbar-actions">
            <BasketBadge @rendermode="InteractiveAuto" />
            <UserMenu />
            <ThemeToggle @rendermode="InteractiveAuto" />
        </div>
    </div>
</header>
```

Three things to get right:

- **`/orders` is inside `AuthorizeView`** — it is `[Authorize]`, so an anonymous visitor clicking it
  would bounce through the OIDC challenge for no reason. That also finally makes the page reachable
  (defect 4).
- **`NavLink` works under static SSR.** It resolves `ActiveClass` from the URI at render time; it needs
  no interactivity. Do not make the whole header an island just for this.
- **`/basket` is not a nav link** — `BasketBadge` already links to it, exactly as the reference does.

`StoreHeader.razor.css` — the layout trick is the reference's: `justify-content: flex-end` on the row
with `margin-right: auto` on the logo.

```css
.eshop-navbar {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    z-index: 2;
    background: transparent;
}

.eshop-navbar-inner {
    max-width: var(--eshop-max);
    margin-inline: auto;
    padding-inline: var(--eshop-gutter);
    min-height: var(--eshop-navbar-height);
    display: flex;
    flex-direction: row;
    justify-content: flex-end;
    align-items: center;
    gap: 1.5rem;
}

.eshop-logo {
    margin-right: auto;
    color: var(--eshop-ink);
    width: 20vw;
    max-width: 250px;
    min-width: 100px;
    display: flex;
}

.eshop-navlinks {
    display: flex;
    gap: 1.5rem;
}

.eshop-navlinks a {
    color: var(--eshop-ink);
    text-decoration: none;
    font-size: var(--eshop-body-size);
    font-weight: var(--eshop-weight-semibold);
}

.eshop-navlinks a.active {
    text-decoration: underline;
    text-underline-offset: 0.4rem;
}

.eshop-navbar-actions {
    display: flex;
    align-items: center;
    gap: 1rem;
}

@media only screen and (max-width: 480px) {
    .eshop-navlinks { display: none; }
}
```

> Nav links are hidden below 480px and the logo + actions remain. That is a deliberate simplification —
> the reference has **no nav links at all**, so there is nothing to fall back to; ours degrade to the
> basket and user menu, which is where a phone user goes anyway.

## `StoreHero`

The component that replaces `SectionOutlet`. See [`00-overview.md`](00-overview.md) for why sections
cannot be used here.

```razor
@namespace Tnosc.EShop.Client.Web.Client.Layout.Store

<div class="eshop-hero @(Tall ? "tall" : "")">
    <div class="eshop-hero-image"></div>
    <div class="eshop-hero-content">
        <h1>@Title</h1>
        @if (!string.IsNullOrWhiteSpace(Subtitle))
        {
            <p>@Subtitle</p>
        }
    </div>
</div>

@code {
    [Parameter, EditorRequired] public required string Title { get; set; }
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public bool Tall { get; set; }
}
```

`StoreHero.razor.css`:

```css
.eshop-hero {
    position: relative;
    height: var(--eshop-hero-short);
    margin-bottom: 4rem;
    overflow: hidden;
}

.eshop-hero.tall {
    height: var(--eshop-hero-tall);
    margin-bottom: 0;
}

.eshop-hero-image {
    position: absolute;
    inset: 0;
    background-image: linear-gradient(to right, var(--eshop-surface) 0%, transparent 70%),
                      url("/images/hero.webp");
    background-size: cover;
    background-position: center;
}

.eshop-hero.tall .eshop-hero-image {
    background-image: linear-gradient(to right, var(--eshop-surface) 0%, transparent 70%),
                      url("/images/hero-home.webp");
}

.eshop-hero-content {
    position: absolute;
    bottom: 3rem;
    max-width: 48rem;
    padding-inline: var(--eshop-gutter);
}

.eshop-hero-content h1 {
    color: var(--eshop-ink);
    font-size: var(--eshop-hero-size);
    font-weight: var(--eshop-weight-bold);
    line-height: var(--eshop-hero-line);
    margin: 0;
}

.eshop-hero-content p {
    color: var(--eshop-ink);
    font-size: var(--eshop-sub-size);
    font-weight: var(--eshop-weight-bold);
    line-height: var(--eshop-sub-line);
    margin: 0;
}
```

The gradient scrim is what keeps ink-coloured text legible over an arbitrary photo, in both themes. The
reference has none and relies on its two photos being pale on the left — do not depend on that.

> The reference sets `white-space: nowrap` on the intro block, which clips long titles. Ours does not —
> product names and order numbers go through here.

## `StoreFooter`

```razor
<footer class="eshop-footer">
    <div class="eshop-footer-inner">
        <span class="eshop-logo-footer">@* inline SVG, currentColor *@</span>
        <p>© Tnosc EShop</p>
    </div>
</footer>
```

```css
.eshop-footer {
    margin-top: 3.5rem;
    background-color: var(--eshop-ink);
    width: 100%;
}

.eshop-footer-inner {
    max-width: var(--eshop-max);
    margin-inline: auto;
    padding: 3.5rem var(--eshop-gutter);
    color: var(--eshop-on-ink);
    display: flex;
    justify-content: flex-end;
    align-items: center;
}

.eshop-logo-footer {
    margin-right: auto;
    color: var(--eshop-on-ink);
    width: 100px;
}
```

Because the band is `--eshop-ink` and the logo is `currentColor: --eshop-on-ink`, it inverts correctly
in dark mode with no second SVG file.

## `UserMenu` — absorbing `LoginDisplay`

The reference's user menu is a **CSS-only hover dropdown, no JS** — which is exactly what a statically
rendered header needs. `LoginDisplay`'s current content becomes its two branches, so `LoginDisplay.razor`
is deleted once `UserMenu` carries both.

```razor
<AuthorizeView>
    <Authorized>
        <div class="eshop-dropdown">
            <span class="eshop-dropdown-button" aria-label="Account">@* user.svg inline *@</span>
            <div class="eshop-dropdown-content">
                <a class="eshop-dropdown-item" href="/orders">My orders</a>
                <form class="eshop-dropdown-item" method="post" action="bff/logout">
                    <AntiforgeryToken />
                    <button type="submit">Sign out</button>
                </form>
            </div>
        </div>
    </Authorized>
    <NotAuthorized>
        <a class="eshop-dropdown-button" href="bff/login" aria-label="Sign in">@* user.svg inline *@</a>
    </NotAuthorized>
</AuthorizeView>
```

Keep the existing sign-out shape exactly: `method="post" action="bff/logout"` with `<AntiforgeryToken />`.
That is a real form post to the BFF, not an `OnClick` — it works under static SSR, which is the whole
reason it survives the rail's removal unchanged. Do not "modernise" it into a `FluentButton OnClick`.

CSS: `.eshop-dropdown { position: relative; }`, `.eshop-dropdown-content { display: none; position:
absolute; right: 0; background: var(--eshop-surface); box-shadow: var(--eshop-shadow-menu); min-width:
8rem; z-index: 3; }`, revealed by `.eshop-dropdown:hover .eshop-dropdown-content`. Add
`:focus-within` alongside `:hover` so it is keyboard-reachable — the reference is hover-only and is not.

## The two render-mode islands

This is what fixes defects 1 and 2.

`BasketBadge` and `ThemeToggle` are declared `@rendermode="InteractiveAuto"` at their call sites in
`StoreHeader`. Both take no parameters, so nothing needs to serialize across the boundary.

- **`BasketBadge`** then runs in the same interactive context as the pages that mutate the basket, so
  its `BasketState` is the same scoped instance and `BasketState.Changed` actually reaches it.
  `BasketBadge.razor.cs` needs **no change** — the subscription, the `AuthenticationStateTask` guard and
  the initial `GetBasketAsync` all stay. Only where it renders changes. Re-render the markup on the
  inline `cart.svg` plus a count element instead of `FluentCounterBadge`, so the badge positions against
  the anchor rather than a distant ancestor (the bug the reference has).
- **`ThemeToggle`** gets a working `OnClick`. Keep `IThemeService.SwitchThemeAsync()`; it is the typed
  service, not raw interop.

Everything else in the header — logo, nav links, `UserMenu`, `AuthorizeView` — stays static SSR. Three
islands in a header would be three separate WASM roots for no gain.

> `AuthorizeView` inside a statically rendered header reads the server-side `HttpContext` principal, so
> the signed-in state is correct on first paint. That is the behaviour today; removing the rail does not
> change it.

## Every store page grows a hero

Each page in `Features/Store/` gains `<StoreHero />` as its first element and wraps the remainder in
`.eshop-page`. `Features/Shared/PageHeader.razor` is superseded on store pages — **do not delete it**,
admin still uses it ([08](08-admin-console.md)).

| Page | `Title` | `Subtitle` | `Tall` |
|---|---|---|---|
| `Home.razor` | "Ready for a new adventure?" | "Start the season with the latest in the catalogue." | `true` |
| `Catalog/Products.razor` | "Catalogue" | "Browse everything in the store." | |
| `Catalog/ProductDetail.razor` | `@_product.Name` | `@_product.BrandName` | |
| `Basket/BasketPage.razor` | "Your shopping bag" | | |
| `Checkout/CheckoutPage.razor` | "Checkout" | | |
| `Orders/MyOrders.razor` | "My orders" | | |
| `Orders/OrderDetail.razor` | `$"Order {_order.OrderNumber}"` | | |

For the two dynamic ones, render the hero **inside** the loaded branch so the title is never blank, and
give the loading branch its own hero with a static title. This task only wires the heroes and the
`.eshop-page` wrapper — the page bodies are restyled in [04](04-catalog-grid.md)–[07](07-orders.md).

---

## The grep gate

Carried over from [`../03-shell-and-layouts.md`](../03-shell-and-layouts.md). An unknown Fluent
component name is `RZ10012` — a **warning**, which does not fail this build, and renders a broken
unknown element instead.

```bash
grep -rnE 'FluentTextField|FluentNumberField|IToastService|FluentNavMenu|FluentNavLink|FluentDesignTheme|Appearance\.Accent|SelectedOptions|IDialogContentComponent' src/client --include=*.razor --include=*.cs
```

Must return zero hits.

---

## Definition of done

- [ ] `StoreNav.razor` and `LoginDisplay.razor` are deleted; nothing references them.
- [ ] The store has **no nav rail**; the header floats over the hero with a transparent background.
- [ ] The footer sits at the bottom of the viewport on a short page (check `/basket` when empty).
- [ ] Every store page renders a hero; Home's is tall, the rest are short; no hero renders a blank `h1`.
- [ ] **Defect 1:** signed in, add an item from `/products/{id}` — the header count increments **without
      a reload**.
- [ ] **Defect 2:** the theme toggle flips light ⇄ dark on click, and the hero, footer and nav follow.
- [ ] **Defect 4:** `/orders` is in the nav for signed-in users and absent for anonymous ones.
- [ ] Sign-out still works — it is still a form post to `bff/logout` carrying `<AntiforgeryToken />`.
- [ ] The user dropdown opens on keyboard focus, not only hover.
- [ ] The grep gate returns zero hits.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.
