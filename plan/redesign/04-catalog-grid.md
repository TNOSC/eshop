# Task 04 — Catalogue grid

**Goal:** `/products` as the reference's catalogue — a `14rem` filter sidebar of pill tags beside a
flex-wrap grid of image-first cards, with square-chip pagination.

**Depends on:** [03](03-store-shell.md). Independent of [05](05-product-detail.md)–[09](09-error-pages.md).

---

## Files to edit

| File | Change |
|---|---|
| `Features/Store/Catalog/Products.razor` (+ `.razor.cs`) | Two-column body; `FluentGrid` → flex-wrap; `FluentPaginator` → chips |
| `Features/Store/Catalog/ProductCard.razor` | Rebuilt image-first |
| `Features/Store/Catalog/ProductFilters.razor` | Select → pill tags |

**Created:** `Products.razor.css`, `ProductCard.razor.css`, `ProductFilters.razor.css`.

---

## The two-column body

The hero is already in place from [03](03-store-shell.md). Below it:

```razor
<div class="eshop-page eshop-catalog">
    <ProductFilters … />
    <div class="eshop-catalog-results">
        <div class="eshop-catalog-items">
            @foreach (ProductSummary product in _products) { <ProductCard Product="@product" /> }
        </div>
        @* pagination *@
    </div>
</div>
```

```css
.eshop-catalog {
    display: flex;
    gap: var(--eshop-col-gap);
}

.eshop-catalog-results { flex-grow: 1; }

.eshop-catalog-items {
    display: flex;
    align-items: flex-start;
    align-content: flex-start;
    gap: var(--eshop-grid-gap);
    flex-wrap: wrap;
}

@media only screen and (max-width: 1024px) {
    .eshop-catalog { flex-direction: column; }
}
```

`FluentGrid`/`FluentGridItem` go away here. The reference's card sizing is `flex-basis: calc(33.33% -
2.5rem)` on a wrapping flex row, and a 12-column grid cannot express the "hover adds a border without
shifting anything" trick cleanly alongside it.

Keep `PageSize` at whatever `Products.razor.cs` uses today, but make sure it is a **multiple of 3** so
the last row is not ragged — the reference uses 9 for exactly this reason. If it is not, change it and
say so in the commit message.

## `ProductCard` — image-first

```razor
<div class="eshop-card" data-testid="product-card">
    <a class="eshop-card-link" href="@($"/products/{Product.Id}")">
        <span class="eshop-card-image">
            <ProductImage Sku="@Product.Sku" Alt="@Product.Name" />
        </span>
        <span class="eshop-card-content">
            <span class="eshop-card-name" data-testid="product-card-name">@Product.Name</span>
            <span class="eshop-card-price">
                <MoneyDisplay Amount="@Product.PriceAmount" Currency="@Product.PriceCurrency" />
            </span>
        </span>
    </a>
    @if (Product.StockQuantity <= 0)
    {
        <span class="eshop-card-flag">Out of stock</span>
    }
</div>
```

Keep both `data-testid` attributes — they are the only test hooks on this component.

```css
.eshop-card {
    flex-basis: calc(33.33% - var(--eshop-grid-gap));
    flex-shrink: 0;
    box-sizing: border-box;
    padding: 2px;
    position: relative;
}

.eshop-card:hover {
    cursor: pointer;
    padding: 0;
    border: 2px solid var(--eshop-ink);
}

.eshop-card-link {
    text-decoration: none;
    display: block;
}

.eshop-card-content {
    display: flex;
    padding: 0 0.75rem;
    align-items: center;
    gap: 0.5rem;
}

.eshop-card-name {
    color: var(--eshop-ink);
    font-size: var(--eshop-body-size);
    font-weight: var(--eshop-weight-semibold);
    line-height: var(--eshop-body-line);
    text-align: left;
}

.eshop-card-price {
    color: var(--eshop-muted);
    margin-left: auto;
    text-align: right;
    font-size: var(--eshop-body-size);
    font-weight: var(--eshop-weight-semibold);
}

@media only screen and (max-width: 480px) {
    .eshop-card { flex-basis: calc(100% - 2rem); }
}

@media only screen and (min-width: 481px) and (max-width: 1024px) {
    .eshop-card { flex-basis: calc(50% - 3rem); }
}
```

Three things this encodes:

- **`padding: 2px` → `border: 2px` on hover.** The border replaces the padding, so the card does not
  shift and its neighbours do not reflow. Do not use `outline` "to be safe" — the swap is the effect.
- **No shadow, no radius, no border at rest.** Flat and square is the design; `FluentCard
  Shadow="CardShadow.Small"` is what we are removing.
- **3 → 2 → 1 columns**, driven by `flex-basis`, not a media query on the container.

The out-of-stock flag replaces `FluentBadge Color="BadgeColor.Danger"`. Position it over the image
corner (`position: absolute`) using `--eshop-status-bad`, and give `.eshop-card` `position: relative` —
which it has above. This is the reference's cart-badge bug done correctly.

`MoneyDisplay` is reused unchanged; if its markup fights the flex row, wrap it rather than editing it —
`Features/Shared/` components are shared with admin.

## `ProductFilters` — pills, not a select

Today it is a `FluentTextInput` search plus a `FluentSelect` of categories, both driven by parent
`EventCallback`s. The reference is a sidebar of link pills whose state lives entirely in the query
string.

**Keep the search box** — the reference has no search anywhere, but Tnosc's `SearchProductsAsync`
supports it and removing a working feature to be faithful is the wrong trade. It stays a
`FluentTextInput TextInputType="TextInputType.Search"` at the top of the sidebar.

**Replace the category select with pills.** `ICatalogApi` exposes `GetCategoriesAsync` only — there is
no brands endpoint — so the sidebar has **one** group, "Category", where the reference has two (Brand,
Type). Do not invent a brand filter; `ProductSummary.BrandName` is display data, not a facet.

```razor
<div class="eshop-filters">
    <div class="eshop-filters-header">@* filters.svg inline *@ Filters</div>
    @* search box *@
    <div class="eshop-filters-group">
        <h3>Category</h3>
        <div class="eshop-filters-tags">
            <a class="eshop-tag @(CategoryId is null ? "active" : "")" href="@CategoryUri(null)">All</a>
            @foreach (Category category in Categories)
            {
                <a class="eshop-tag @(CategoryId == category.Id ? "active" : "")"
                   href="@CategoryUri(category.Id)">@category.Name</a>
            }
        </div>
    </div>
</div>
```

```css
.eshop-filters { flex-shrink: 0; width: 14rem; }

.eshop-filters-header { display: flex; align-items: center; gap: 0.7rem; }

.eshop-filters-group h3 {
    color: var(--eshop-ink);
    font-size: var(--eshop-body-size);
    font-weight: var(--eshop-weight-semibold);
    line-height: var(--eshop-body-line);
}

.eshop-filters-tags {
    border-top: 1px solid var(--eshop-rule);
    display: flex;
    padding: 0.75rem 0;
    align-items: center;
    gap: 0.25rem;
    flex-wrap: wrap;
}

.eshop-tag {
    display: flex;
    padding: 0.5rem 0.75rem;
    align-items: center;
    border-radius: var(--eshop-radius-pill);
    color: var(--eshop-muted);
    font-size: var(--eshop-body-size);
    font-weight: var(--eshop-weight-regular);
    text-decoration: none;
}

.eshop-tag:hover { cursor: pointer; background: var(--eshop-hover); }
.eshop-tag.active { background: var(--eshop-ink); color: var(--eshop-on-ink); }

@media only screen and (max-width: 1024px) {
    .eshop-filters { width: 100%; }
    .eshop-filters-tags { justify-content: space-between; }
}

@media only screen and (max-width: 480px) {
    .eshop-filters-header { display: none; }
}
```

### Query-string state

`CategoryUri` builds the href with `NavigationManager.GetUriWithQueryParameters`, and **must reset
paging** — the reference's one non-obvious detail:

```csharp
private string CategoryUri(Guid? categoryId) =>
    Navigation.GetUriWithQueryParameters(new Dictionary<string, object?>
    {
        { "page", null },
        { "category", categoryId },
    });
```

Landing on page 4 of "All" and clicking a category with three products otherwise shows an empty grid.

This moves category state from a parent `EventCallback` to the URL, so `Products.razor.cs` gains
`[SupplyParameterFromQuery]` for `category` and `page` and drops `OnCategoryIdChangedAsync`. Keep the
search on its existing `EventCallback` path — it is `Immediate="true"` with a 300ms delay and pushing
every keystroke into the URL would spam history. Note the asymmetry in a comment so the next reader
does not "fix" it.

Pills are plain `<a href>`, so **filtering works with no interactivity at all** — which is the
reference's design and survives prerender cleanly.

## Pagination — chips

`FluentPaginator` goes; the reference renders page numbers as square chips, the active one inverted.

```css
.eshop-pages { display: flex; align-items: center; gap: 0.5rem; justify-content: center; margin-top: 1.5rem; }

.eshop-pages a {
    display: flex;
    padding: 12px 20px;
    align-items: center;
    background: var(--eshop-panel);
    color: var(--eshop-ink);
    text-decoration: none;
}

.eshop-pages a.active { background: var(--eshop-ink); color: var(--eshop-panel); }
```

Scope the selector to `.eshop-pages a` — **not** a bare `::deep a`, which is the leak the reference
has. Build hrefs with `GetUriWithQueryParameter("page", …)`, passing `null` for page 1 so the canonical
URL has no `?page=1`.

## Loading and empty states

- Loading: keep the skeleton approach, but size the placeholders like cards — `flex-basis` matching
  `.eshop-card` and an image-shaped block on top, so the grid does not jump when data arrives.
- Empty: the reference has no empty state. Ours keeps one, restyled — plain text in `--eshop-muted`
  inside `.eshop-catalog-results`, not a second `PageHeader` (which would render an `h1` under the
  hero's `h1`).
- Errors: keep `ErrorPanel` + `ErrorCodeMessages.Humanize(_problem)` exactly as-is.

---

## Definition of done

- [ ] `/products` renders a `14rem` sidebar and a 3-up card grid; at 1024px 2-up with the sidebar above;
      at 480px 1-up, full-width filters, "Filters" header hidden.
- [ ] Cards show an image, name left, price right, square corners, no shadow.
- [ ] Hovering a card draws a 2px border and **nothing shifts**.
- [ ] Out-of-stock renders a flag over the image, positioned against the card.
- [ ] Category pills change the URL, the active pill inverts, and **switching category resets to page 1**.
- [ ] Pagination chips work and page 1 has no `?page=1`.
- [ ] Filtering and paging work **with JavaScript disabled** (they are plain links).
- [ ] `FluentGrid`, `FluentGridItem`, `FluentPaginator`, `FluentSelect` and `FluentCard` no longer appear
      in these three files; the search `FluentTextInput` remains.
- [ ] No bare `::deep` selector, no hard-coded colour.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.
