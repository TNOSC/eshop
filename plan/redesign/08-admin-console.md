# Task 08 — Admin console

**Goal:** bring the back office onto the same token layer without giving it the storefront's shell, and
fix the missing dialog provider that makes three dialogs fail silently today.

**Depends on:** [01](01-design-tokens.md). Independent of [04](04-catalog-grid.md)–[07](07-orders.md).

---

## The console keeps its rail — on purpose

The storefront drops `FluentLayout` because a full-bleed hero and footer fight its grid. **None of that
applies here.** A back office with five destinations wants a persistent rail, has no hero, no footer and
no marketing surface. `AdminLayout` therefore keeps `FluentLayout`, `LayoutArea.Navigation` and
`AdminNav`'s `FluentNavCategory` structure exactly as they are.

What it adopts is the **token layer** — colour, type scale and rules — so the two halves of the app do
not read as two applications, and so the theme toggle works consistently.

`FluentDataGrid` **stays** in the admin pages. It is the right control for a back-office table, and the
reference has no admin surface to imitate. Only `/basket` and `/orders` lose theirs, because those are
storefront pages the reference designs explicitly.

---

## Files to edit

| File | Change |
|---|---|
| `Layout/Admin/AdminLayout.razor` | **Add `<FluentDialogProvider />`** (defect 3); inline header on tokens |
| `Layout/Admin/AdminNav.razor` | Tokens only; structure unchanged |
| `Features/Admin/AdminDashboard.razor` | Stub → tiles |
| `Features/Admin/Catalog/AdminProducts.razor` | Grid styling on tokens |
| `Features/Admin/Identity/AdminCustomers.razor`, `AdminCustomerDetail.razor` | Same |
| `Features/Admin/Catalog/{CreateProduct,UpdateProductPrice,AdjustStock}Dialog.razor` | Same |
| `Features/Shared/PageHeader.razor` | Retained for admin; restyled on tokens |

**Created:** `AdminLayout.razor.css`, `AdminDashboard.razor.css`.

---

## Step 1 — defect 3, first and on its own

`AdminLayout.razor` ends with:

```razor
<FluentProviders />
<FluentToastProvider />
```

while `AdminProducts.razor` opens three dialogs through `IDialogService`. Without
`FluentDialogProvider` in the rendered tree, `ShowDialogAsync` **fails silently** — no exception,
nothing appears. Match `StoreLayout`:

```razor
<FluentProviders />
<FluentToastProvider />
<FluentDialogProvider />
<FluentTooltipProvider />
```

Verify by opening the create-product dialog **before** doing any styling work, so the fix is proven
independently of everything else in this task.

## Step 2 — the header row

`AdminLayout`'s header is an inline `FluentStack` referencing `ThemeToggle` and `LoginDisplay` from
`Layout.Store`. [03](03-store-shell.md) deletes `LoginDisplay` (absorbed into `UserMenu`) and makes
`ThemeToggle` an island at its call site — **so this file will not compile after [03] unless it is
updated too.**

If [03] lands first, this is a broken build, not a styling nicety. Fix it here:

- Replace `<LoginDisplay />` with `<UserMenu />`.
- Declare the toggle as an island: `<ThemeToggle @rendermode="InteractiveAuto" />`. The admin layout is
  statically rendered for the same reason the store layout is, so the toggle is inert here too until it
  gets its own island — defect 2 applies to both layouts.

> If [03](03-store-shell.md) has **not** landed when you start this task, do Step 1, then stop and do
> [03] first. Splitting the `LoginDisplay` removal across two tasks is what breaks the build.

Keep `FluentLayoutHamburger`, the `/admin` anchor and `FluentSpacer` as they are.

## Step 3 — tokens

Nothing structural. Add `AdminLayout.razor.css` and set the header row, the rail and the content area on
`--eshop-*` values so the console follows the theme:

- Header band and rail background: `--eshop-surface`; divider `--eshop-rule`.
- The "Tnosc EShop — Back office" anchor: `--eshop-ink`, `--eshop-body-size`,
  `--eshop-weight-semibold`, no underline.
- Grid header rows and section headings: `.eshop-h2` where a heading exists.
- `PageHeader.razor`: its title on `--eshop-h2-size`/`--eshop-weight-semibold`, subtitle on
  `--eshop-muted`. It is now admin-only, but **leave its parameter surface alone** — bUnit tests do not
  exist yet, but the component is referenced from five pages.

Dialogs: their fields are `FluentTextInput` / `FluentNumberInput`, which keep Fluent's own styling. Only
the dialog's own headings and helper text move onto tokens. **Do not restyle Fluent form controls** —
that is where a token layer starts fighting the component library.

## Step 4 — `AdminDashboard`

It is a stub: a single `PageHeader` and nothing else. Fill it with a tile row linking to the two real
destinations, using the counts already reachable from the existing clients.

```razor
<div class="eshop-admin-tiles">
    <a class="eshop-admin-tile" href="/admin/products">
        <span class="label">Products</span>
        <span class="value">@(_productCount?.ToString() ?? "—")</span>
    </a>
    <a class="eshop-admin-tile" href="/admin/customers">
        <span class="label">Customers</span>
        <span class="value">@(_customerCount?.ToString() ?? "—")</span>
    </a>
</div>
```

- Counts come from the **existing** `ICatalogApi.SearchProductsAsync` and
  `IIdentityApi.SearchCustomersAsync` — request page size 1 and read the total off the paged result.
  **Do not add an endpoint, a client method or a contract for this.** If neither paged result exposes a
  total, render the tiles without a value rather than adding server work; say so in the commit message.
- Both calls go through the usual `ApiResult<T>` path; on failure render `—`, not an error panel — a
  dashboard tile is not worth blocking the page for.
- Tiles are square, flat `--eshop-panel`, `--eshop-ink` text, no radius, no shadow. Hover swaps to
  `--eshop-hover`. Same visual family as the storefront's summary panel.
- Keep `@attribute [Authorize(Roles = "admin")]`. It is **not** inherited from
  `Features/Admin/_Imports.razor` — only `@layout`, `@using`, `@inject` and `@namespace` propagate. This
  is restated in [`../03-shell-and-layouts.md`](../03-shell-and-layouts.md) and it catches people every
  time.

---

## Definition of done

- [ ] **Defect 3:** all three product dialogs open from `/admin/products` — create, reprice, adjust
      stock — and each still raises its toast.
- [ ] `AdminLayout` renders `FluentProviders`, `FluentToastProvider`, `FluentDialogProvider` and
      `FluentTooltipProvider`.
- [ ] The admin header references `UserMenu` and an island `ThemeToggle`; the build is clean after
      [03](03-store-shell.md)'s deletions.
- [ ] The theme toggle works **in the admin console**, and the rail, header and grids follow.
- [ ] `FluentLayout`, `LayoutArea.Navigation` and `AdminNav`'s categories are unchanged.
- [ ] `FluentDataGrid` is still used on all three admin list pages.
- [ ] `/admin` shows tiles with counts (or `—`), each linking to its section.
- [ ] Every admin page still carries its own `@attribute [Authorize(Roles = "admin")]`.
- [ ] No new endpoint, client method or contract was added.
- [ ] No hard-coded colour in the new CSS.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.
