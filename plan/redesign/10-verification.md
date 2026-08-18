# Task 10 — Verification

**Goal:** the closing gate. Nothing new is built here; this is the checklist that says the series is
done and nothing regressed.

**Depends on:** [01](01-design-tokens.md)–[09](09-error-pages.md), all green.

---

## 1. Build and the server suites

```bash
dotnet build Tnosc.EShop.slnx                                    # warnings are errors
dotnet test  tests/server/Tnosc.EShop.Server.Tests.Architecture  # must stay green
dotnet test  Tnosc.EShop.slnx                                    # integration suite needs Docker
```

The architecture suite does not scan the client — `Tests.Architecture.csproj` enumerates 13 explicit
`ProjectReference`s and `ConfigurationTests` names eight assemblies by hand, none of them client
projects. It must still pass, because **this series must not have touched the server at all.** Confirm:

```bash
git diff --stat main -- src/server lib aspire tests
```

Must be empty. If it is not, something in this series reached past its scope — the product-image
decision in [02](02-brand-assets.md) exists precisely so it would not.

## 2. The two greps

**Unknown Fluent component names.** `RZ10012` is a *warning*, so a misspelled component compiles clean
and renders a broken unknown HTML element. This is the single most likely silent failure in a
markup-heavy change.

```bash
grep -rnE 'FluentTextField|FluentNumberField|IToastService|FluentNavMenu|FluentNavLink|FluentDesignTheme|Appearance\.Accent|SelectedOptions|IDialogContentComponent' src/client --include=*.razor --include=*.cs
```

**Hard-coded colour outside the token block.**

```bash
grep -rnE '#[0-9a-fA-F]{3,6}\b|\b(white|black|lightyellow|red|green|blue)\b' src/client --include=*.css | grep -v 'wwwroot/app.css'
```

Both must return zero. The `app.css` exclusion covers the `:root` block and the documented
`#blazor-error-ui` exception from [01](01-design-tokens.md); nothing else may hold a literal.

**Bare `::deep`.** A `::deep` not anchored to one of our classes leaks styling to every descendant — the
bug the reference has in `Catalog.razor.css`.

```bash
grep -rnE '^\s*::deep|[^-a-zA-Z0-9_]\s+::deep' src/client --include=*.css
```

Review each hit by hand: `.eshop-item ::deep .eshop-product-image` is fine, `::deep a` is not.

## 3. Responsive pass

At **1440px**, **1024px**, **768px** and **375px**, on `/`, `/products`, `/products/{id}`, `/basket`,
`/checkout`, `/orders`:

- [ ] **No horizontal scrollbar at any width.** The most likely offender is a hero `background-size:
      cover` on a `120rem` band, or a `.eshop-cart-row` whose number input will not shrink.
- [ ] Catalogue grid is 3-up ≥1025px, 2-up 481–1024px, 1-up ≤480px.
- [ ] Filter sidebar is `14rem` on desktop, full-width below 1024px, "Filters" header hidden ≤480px.
- [ ] Basket flips to `column-reverse` below 1024px — summary **above** items.
- [ ] Product detail stacks below 1024px with a full-width image.
- [ ] Hero title drops to `2.5rem` / `2rem` at the two breakpoints and does not clip.
- [ ] Nav links hide ≤480px; logo, basket and user menu remain.

## 4. Theme pass

Toggle light ⇄ dark **on every page**, storefront and admin. This is the check that fails if any
hard-coded colour survived the greps by living in an inline `style` attribute.

- [ ] Hero scrim keeps the `h1` legible over the photo in **both** themes.
- [ ] Footer band inverts (`--eshop-ink` background, `--eshop-on-ink` logo).
- [ ] Filter pills, status pills, cart summary panel, pagination chips and admin tiles all follow.
- [ ] The reconnect modal follows (it did not before [01](01-design-tokens.md)).
- [ ] Nothing renders ink-on-ink or white-on-white anywhere.

## 5. Accessibility pass

- [ ] **Exactly one `<h1>` per page** — the hero's. `ProductDetail` had a second one; [05](05-product-detail.md)
      removes it. `FocusOnNavigate Selector="h1"` in `Routes.razor` depends on this.
- [ ] Every product image has a real `alt` (the product name), not `role="presentation"`.
- [ ] The user dropdown opens on **keyboard focus**, not hover only.
- [ ] Every order is reachable by tab — the order number is an `<a>`, not a row click.
- [ ] Icon-only buttons keep a `Title`/`aria-label` (basket, remove line, theme toggle).
- [ ] Contrast holds in both themes; the pill colours are the ones to check, since they are the only
      chromatic values in the palette.

## 6. No-JavaScript pass

Disable JavaScript and load `/products`. The reference's catalogue is entirely link-driven, and
[04](04-catalog-grid.md) preserved that:

- [ ] The grid renders (it is prerendered).
- [ ] Category pills filter.
- [ ] Pagination chips page.
- [ ] `/` , `/products/{id}` and `/not-found` render.

The basket, checkout and orders pages are `prerender: false` and will not render — that is expected and
unchanged.

## 7. Functional walkthrough

```bash
dotnet run --project aspire/Tnosc.EShop.AppHost   # Docker required
```

From the Aspire dashboard, open `eshop-web`:

1. **Anonymous** — tall hero on `/`, short hero elsewhere. Cards show images; hovering draws a 2px
   border **without shifting layout**. Filter pills change the URL, the active pill inverts, and
   switching category **resets to page 1**. `/products/{id}` shows image left, price and buy row right,
   "Sign in to buy" instead of a stepper. `/orders` is **not** in the nav.
2. **`customer@eshop.local` / `Passw0rd!`** — `/orders` appears in the nav (**defect 4**). Add an item:
   the header count increments **without a reload** (**defect 1**). `/basket` shows the pseudo-table and
   the flat summary; change a quantity, remove a middle line, confirm no stale quantity elsewhere.
   `/checkout` → place order → `/orders` lists it with an **outlined** status pill.
3. **`admin@eshop.local` / `Passw0rd!`** — `/admin` shows tiles; the console keeps its rail. The
   create-product dialog **opens** (**defect 3** — it failed silently before). Create, reprice, adjust
   stock; each raises a toast. Idempotency still holds: submit the create dialog twice from the same
   open dialog and exactly one product exists.
4. **The theme toggle flips on click** (**defect 2**), in both the store and the admin console.
5. **Dev tools → Network** — requests still go to `/bff/api/…` on the web app's own origin, and **no
   `Authorization` header and no token appear in the browser**. This is the BFF's whole point and the
   one thing not verifiable server-side; a shell rewrite is exactly the kind of change that could break
   it by accident.
6. **`/not-found` and a forced `/Error`** render inside the design system, with a hero and no template
   boilerplate.

---

## What is not covered, and why

There are **no UI tests.** [`../11-bunit-tests.md`](../11-bunit-tests.md) describes a
`tests/client/Tnosc.EShop.Client.Tests.Unit` project that was never created; `tests/` holds only the
four server suites. So nothing mechanically guards this redesign, and the walkthrough above **is** the
regression suite.

If that becomes uncomfortable, the right follow-up is to build the bUnit project from that task file and
cover the three things a human check is worst at: `ProductImageResolver.For` determinism,
`OrderStatusPill`'s status → token mapping including the unknown-status default, and `StoreHeader`
rendering `/orders` only when authorized. That is a separate piece of work, not a step here.

---

## Definition of done

- [ ] `dotnet build Tnosc.EShop.slnx` clean; architecture and integration suites green.
- [ ] `git diff --stat main -- src/server lib aspire tests` is empty.
- [ ] All three greps return zero (the `::deep` one after manual review).
- [ ] Sections 3–7 pass, every box ticked.
- [ ] All four defects from [`00-overview.md`](00-overview.md) are demonstrably fixed.
- [ ] `README` / `docs/decisions/` updated if the shell change contradicts anything written there.
