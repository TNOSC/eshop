# Task 06 — Basket and checkout

**Goal:** `/basket` as the reference's two-column shopping bag — a flex pseudo-table of lines beside a
flat summary panel — and `/checkout` as its stacked form sections with a rule-separated button bar.

**Depends on:** [03](03-store-shell.md). Independent of [04](04-catalog-grid.md), [05](05-product-detail.md),
[07](07-orders.md).

---

## Files to edit

| File | Change |
|---|---|
| `Features/Store/Basket/BasketPage.razor` | `FluentDataGrid` → flex pseudo-table + summary panel |
| `Features/Store/Basket/BasketLineRow.razor` | Row content for the new shape |
| `Features/Store/Checkout/CheckoutPage.razor` | `FluentCard` stack → form sections + button bar |

**Created:** `BasketPage.razor.css`, `BasketLineRow.razor.css`, `CheckoutPage.razor.css`.

Both pages are `@rendermode @(new InteractiveAutoRenderMode(prerender: false))` with
`@attribute [Authorize]`. **Neither changes** — the no-prerender decision is from
[`../10-skeletons.md`](../10-skeletons.md) and still holds.

`PageHeader` is replaced by the hero wired in [03](03-store-shell.md). Delete the `<PageHeader … />`
line from both; do not delete the component.

---

## `/basket` — the pseudo-table

The reference builds its cart as flex rows, not a `<table>`: a header row of column labels, then one row
per line, each with a bottom rule. Columns are **60% / flex-grow / auto**.

```razor
<div class="eshop-page eshop-cart">
    <div class="eshop-cart-items">
        <div class="eshop-cart-header">
            <div class="eshop-cart-info">Products</div>
            <div class="eshop-cart-qty">Quantity</div>
            <div class="eshop-cart-total">Total</div>
        </div>

        @foreach (BasketItem item in _basket.Items)
        {
            <BasketLineRow Item="@item"
                           QuantityChanged="@(quantity => ChangeQuantityAsync(item: item, quantity: quantity))"
                           Remove="@(() => RemoveItemAsync(item: item))"
                           @key="item.Sku" />
        }
    </div>

    <aside class="eshop-cart-summary">
        <div class="eshop-cart-summary-inner">
            <div class="eshop-cart-summary-header">
                @* cart.svg inline *@
                Your shopping bag
                <span class="eshop-cart-count">@_basket.Items.Count</span>
            </div>
            <div class="eshop-cart-summary-total">
                <div>Total</div>
                <div><MoneyDisplay Amount="@totalAmount" Currency="@totalCurrency" /></div>
            </div>
            <a class="eshop-button eshop-button-primary" href="/checkout">Check out</a>
            <a class="eshop-cart-summary-link" href="/products">
                @* arrow-left.svg inline *@ Continue shopping
            </a>
        </div>
    </aside>
</div>
```

`@key="item.Sku"` matters: without it, removing a middle line makes Blazor reuse the wrong component
instance and the quantity inputs show stale values.

```css
.eshop-cart { display: flex; gap: var(--eshop-col-gap); }

.eshop-cart-items {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
    flex: 1 0 0;
}

.eshop-cart-header {
    display: flex;
    padding: 0.5rem 0;
    align-items: center;
    align-self: stretch;
    border-bottom: 1px solid var(--eshop-rule);
    color: var(--eshop-ink);
    font-size: var(--eshop-body-size);
    font-weight: var(--eshop-weight-semibold);
}

.eshop-cart-info { flex-basis: 60%; }
.eshop-cart-qty { flex: 1 0 0; }
.eshop-cart-total { margin-left: auto; text-align: right; }

.eshop-cart-summary-inner {
    display: flex;
    padding: 1rem 1.5rem;
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
    flex-shrink: 0;
    background: var(--eshop-panel);
}

.eshop-cart-summary-header {
    display: flex;
    padding: 0.5rem 0;
    justify-content: space-between;
    align-items: center;
    align-self: stretch;
    gap: 0.5rem;
    border-bottom: 1px solid var(--eshop-ink);
    color: var(--eshop-ink);
    font-size: var(--eshop-h2-size);
    font-weight: var(--eshop-weight-semibold);
    line-height: 120%;
}

.eshop-cart-count {
    margin-left: auto;
    background: var(--eshop-ink);
    color: var(--eshop-on-ink);
    border-radius: var(--eshop-radius-badge);
    min-width: 3.5rem;
    height: 1.5rem;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: var(--eshop-body-size);
    font-weight: var(--eshop-weight-semibold);
}

.eshop-cart-summary-total {
    display: flex;
    justify-content: space-between;
    align-self: stretch;
}

.eshop-cart-summary-link {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--eshop-ink);
    text-decoration: none;
}

@media only screen and (max-width: 1024px) {
    .eshop-cart { flex-direction: column-reverse; }
}
```

**`column-reverse`, not `column`.** Below 1024px the summary — total and the checkout button — moves
**above** the line items. That is deliberate in the reference and worth keeping: on a phone the action
should not be a scroll away.

The summary panel is flat `--eshop-panel`, **no radius and no shadow**. That is the design, not an
oversight.

### `BasketLineRow`

Becomes the row itself rather than a cell's contents, so it renders the same three columns as the
header. Its parameters — `Item`, `QuantityChanged`, `Remove` — **do not change**.

```razor
<div class="eshop-cart-row">
    <div class="eshop-cart-info">
        <ProductImage Sku="@Item.Sku" Alt="@Item.ProductName" />
        <div>
            <p class="name">@Item.ProductName</p>
            <p class="price"><MoneyDisplay Amount="@Item.UnitPriceAmount" Currency="@Item.UnitPriceCurrency" /></p>
        </div>
    </div>

    <div class="eshop-cart-qty">
        <FluentNumberInput TValue="int" Min="1"
                           Value="@Item.Quantity"
                           ValueChanged="@(quantity => QuantityChanged.InvokeAsync(quantity))"
                           StepButtons="NumberInputStepVisibility.Visible" />
        <FluentButton Appearance="ButtonAppearance.Transparent"
                      IconStart="@(new Icons.Regular.Size20.Delete())"
                      Title="Remove"
                      OnClick="@(() => Remove.InvokeAsync())" />
    </div>

    <div class="eshop-cart-total">
        <MoneyDisplay Amount="@(Item.UnitPriceAmount * Item.Quantity)" Currency="@Item.UnitPriceCurrency" />
    </div>
</div>
```

- `.eshop-cart-row` repeats the header's flex shape plus `border-bottom: 1px solid var(--eshop-rule)`
  and `padding-bottom: 1.25rem`.
- `.eshop-cart-info` inside a row is `display: flex; align-items: center; gap: 1.25rem;` and its image
  caps at `max-width: 12rem; max-height: 12rem;`.
- **Drop the `Style="width: 100px;"` inline style** from the number input — it is the only inline style
  in the client; move it to the scoped CSS as `.eshop-cart-qty ::deep [part]`-free sizing on the
  wrapper, or simply constrain `.eshop-cart-qty` itself.
- The Remove button becomes icon-only (the label is redundant next to a trash glyph in a table row).
  Keep `Title="Remove"` — it is the accessible name.

### Empty state

Keep it, restyled: the reference's wording is *"Your shopping bag is empty. Continue shopping."*. Render
plain text plus a link inside `.eshop-page`, not a `FluentStack` of `FluentText` + `FluentAnchorButton`.
The hero above it already says "Your shopping bag", so do not repeat the title.

---

## `/checkout` — form sections

Today the page is two `FluentCard`s and a button. The reference is a single column of `form-section`s
separated by underlined `h2`s, with the actions in a bar separated by a top rule: secondary left,
primary right.

Tnosc's checkout has **no address form** — it reads the customer's default address and refuses if there
is none. So section one is a read-only address block, not the reference's input grid. Keep the
`_customer.DefaultAddressId is null` branch and its message exactly as they are; only the styling
changes.

```razor
<div class="eshop-page eshop-checkout">
    <section class="eshop-form-section">
        <h2 class="eshop-h2">Delivery address</h2>
        <p>@address.Street</p>
        <p>@address.PostalCode @address.City, @address.Country</p>
    </section>

    <section class="eshop-form-section">
        <h2 class="eshop-h2">Items</h2>
        @foreach (BasketItem item in _basket.Items)
        {
            <div class="eshop-checkout-line">
                <span>@item.ProductName × @item.Quantity</span>
                <MoneyDisplay Amount="@(item.UnitPriceAmount * item.Quantity)" Currency="@item.UnitPriceCurrency" />
            </div>
        }
        <div class="eshop-checkout-line total">
            <span>Total</span>
            <MoneyDisplay Amount="@totalAmount" Currency="@totalCurrency" />
        </div>
    </section>

    <div class="eshop-form-buttons">
        <a class="eshop-button eshop-button-secondary" href="/basket">
            @* arrow-left.svg inline *@ Back to the shopping bag
        </a>
        <FluentButton Appearance="ButtonAppearance.Primary"
                      Loading="@_isPlacingOrder"
                      Disabled="@_isPlacingOrder"
                      OnClick="@PlaceOrderAsync">
            Place order
        </FluentButton>
    </div>
</div>
```

```css
.eshop-checkout { display: flex; flex-direction: column; gap: 2.5rem; }

.eshop-form-section {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
    align-self: stretch;
}

.eshop-checkout-line {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.eshop-checkout-line.total {
    border-top: 1px solid var(--eshop-rule);
    padding-top: 0.75rem;
    font-weight: var(--eshop-weight-semibold);
}

.eshop-form-buttons {
    display: flex;
    padding: 1.5rem 0;
    justify-content: space-between;
    align-items: center;
    align-self: stretch;
    border-top: 1px solid var(--eshop-ink);
}
```

`FluentDivider` goes — the `.total` top rule replaces it.

**Keep `PlaceOrderAsync` untouched.** It is the only thing on this page that talks to the API, and the
order-placement path including its `Idempotency-Key` handling is settled.

---

## Definition of done

- [ ] `/basket` renders the pseudo-table with 60% / grow / auto columns and a rule under every row.
- [ ] Line thumbnails render and cap at `12rem`.
- [ ] The summary panel is flat `--eshop-panel`, square, with count badge, total, Check out and
      Continue shopping.
- [ ] Below 1024px the summary appears **above** the items (`column-reverse`).
- [ ] Changing a quantity and removing a line still work, and removing a middle line does not leave a
      stale quantity in another row (the `@key`).
- [ ] The header basket count still updates after a change (regression on [03](03-store-shell.md)).
- [ ] `/checkout` renders underlined section headings and the rule-separated button bar.
- [ ] The "no default address" and "empty basket" branches still render and still block checkout.
- [ ] Placing an order still works end to end.
- [ ] `FluentDataGrid`, `FluentCard` and `FluentDivider` no longer appear in these three files.
- [ ] No inline `Style="…"` remains in `BasketLineRow`.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.
