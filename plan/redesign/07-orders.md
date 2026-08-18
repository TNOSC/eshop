# Task 07 — Orders

**Goal:** `/orders` as the reference's four-column list with outlined status pills, and `/orders/{id}`
restyled on the same rules.

**Depends on:** [03](03-store-shell.md). Independent of [04](04-catalog-grid.md)–[06](06-basket-and-checkout.md).

---

## Files to edit

| File | Change |
|---|---|
| `Features/Store/Orders/MyOrders.razor` (+ `.razor.cs`) | `FluentDataGrid` → `<ul>` list; `FluentBadge` → outlined pill |
| `Features/Store/Orders/OrderDetail.razor` | Section headings + pill, on tokens |
| `Features/Store/Checkout/OrderPlaced.razor` | Banner on tokens |

**Created:** `MyOrders.razor.css`, `OrderDetail.razor.css`, `OrderStatusPill.razor` (+ `.razor.css`).

Both pages keep `@rendermode @(new InteractiveAutoRenderMode(prerender: false))` and
`@attribute [Authorize]`. Remove the `<PageHeader … />` line — the hero from [03](03-store-shell.md)
replaces it.

---

## `OrderStatusPill` — extract it first

Both pages render a status, and [08](08-admin-console.md) may want it too. One component, one mapping,
so a new status cannot be styled two different ways.

```razor
@namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders

<span class="eshop-pill" style="@($"--eshop-pill-color: {ColorToken};")">@Status</span>

@code {
    [Parameter, EditorRequired] public required string Status { get; set; }

    private string ColorToken => Status.ToUpperInvariant() switch
    {
        "CANCELLED" => "var(--eshop-status-bad)",
        "CONFIRMED" or "PAID" => "var(--eshop-status-good)",
        _ => "var(--eshop-status-neutral)",
    };
}
```

`.eshop-pill` is already global from [01](01-design-tokens.md) — outlined, never filled, `1.25rem`
radius, `0.75rem/400`. Do not redefine it here.

Two things to get right:

- **Match on `ToUpperInvariant()`, and default to neutral.** The reference switches on a CSS class built
  from `@order.Status.ToLower()`, so a status it has no rule for renders unstyled. A `switch` expression
  with a `_` arm cannot do that. Confirm the actual status strings against
  `Contracts/Ordering/OrderSummary.cs` before writing the arms — do not guess the vocabulary.
- **`ToUpperInvariant`, not `ToUpper`.** `CA1308`/`MA0011`-family analyzer rules are on, and culture-
  sensitive casing on a protocol value is exactly what they exist to catch.

## `/orders` — the list

`FluentDataGrid` + `FluentPaginator` go. The reference uses a `<ul>` whose rows are four flex cells:
Number / Date / Total (right-aligned) / Status.

```razor
<div class="eshop-page">
    <ul class="eshop-orders">
        <li class="eshop-orders-row header">
            <div>Number</div><div>Date</div><div class="right">Total</div><div>Status</div>
        </li>
        @foreach (OrderSummary order in _orders)
        {
            <li class="eshop-orders-row">
                <div><a href="@($"/orders/{order.Id}")">@order.OrderNumber</a></div>
                <div>@order.PlacedOnUtc.ToString("g")</div>
                <div class="right"><MoneyDisplay Amount="@order.TotalAmount" Currency="@order.TotalCurrency" /></div>
                <div><OrderStatusPill Status="@order.Status" /></div>
            </li>
        }
    </ul>
</div>
```

```css
.eshop-orders { list-style: none; margin: 0; padding: 0; }

.eshop-orders-row {
    display: flex;
    align-items: center;
    gap: 1.75rem;
    padding: 1rem 0;
    border-bottom: 1px solid var(--eshop-rule);
}

.eshop-orders-row > div { flex: 1 0 0; }

.eshop-orders-row.header {
    color: var(--eshop-ink);
    font-size: var(--eshop-body-size);
    font-weight: var(--eshop-weight-semibold);
    padding-top: 0;
    padding-bottom: 0.5rem;
}

.eshop-orders-row .right { text-align: right; }

.eshop-orders-row a { color: var(--eshop-ink); text-decoration: none; font-weight: var(--eshop-weight-semibold); }
.eshop-orders-row a:hover { text-decoration: underline; }

@media only screen and (max-width: 480px) {
    .eshop-orders-row { flex-wrap: wrap; gap: 0.5rem; }
    .eshop-orders-row.header { display: none; }
}
```

**Navigation moves from the row to the order number.** Today it is
`OnRowClick="@(row => Navigation.NavigateTo($"/orders/{row.Item?.Id}"))"` — a whole-row click handler
that no keyboard user can reach and that needs interactivity. A real `<a href>` is keyboard-navigable,
middle-clickable and works without JS. Drop `OnRowClick` and the `Navigation` injection if nothing else
uses it.

The header row is hidden below 480px, where the labels would wrap past usefulness.

### Paging

`_ordersProvider` is a `GridItemsProvider` shaped for `FluentDataGrid`. Replacing the grid means
replacing it with a plain call to `IOrderingApi.GetMyOrdersAsync` in `OnInitializedAsync`, holding the
page in a field, and rendering chips like [04](04-catalog-grid.md)'s `.eshop-pages`.

Reuse `.eshop-pages` from `Products.razor.css` by **promoting it to `app.css`** when this task lands, if
[04](04-catalog-grid.md) is already done — two copies of a pagination rule is exactly the drift the
token layer exists to prevent. If [04] is not done yet, define it here and let [04] reuse it; whichever
lands second promotes it. Say which happened in the commit message.

Keep the empty state (`EmptyContent` today): plain text, "You have not placed any orders yet." plus a
link to `/products`.

## `/orders/{id}` and `OrderPlaced`

Lighter touch — these keep their `FluentCard`-free structure but adopt the same vocabulary:

- Status renders through `OrderStatusPill`, not `FluentBadge`.
- Section headings ("Delivery address", "Items") use `.eshop-h2`.
- Line items use the same `.eshop-checkout-line` shape as [06](06-basket-and-checkout.md); promote that
  rule to `app.css` if both tasks are done, under the same rule as `.eshop-pages` above.
- Confirm / Cancel become the `.eshop-form-buttons` bar: secondary left, primary right, top rule.
- `OrderPlaced.razor`'s banner uses `--eshop-status-good` for its border and `--eshop-panel` for its
  background. Keep the `Placed == true` query-parameter mechanism exactly as it is.

**Do not touch** `ConfirmOrderAsync` / `CancelOrderAsync` or their error handling.

---

## Definition of done

- [x] `/orders` renders a four-column list with a rule under every row and the header hidden below 480px.
- [x] Status pills are **outlined, never filled**, and an unrecognised status renders neutral rather
      than unstyled.
- [x] The order number is a real `<a href>`; the row-click handler is gone; keyboard tab reaches every
      order.
- [x] Paging works and the empty state renders.
- [x] `/orders/{id}` uses `OrderStatusPill` and `.eshop-h2`; Confirm and Cancel still work.
- [x] `FluentDataGrid`, `FluentPaginator` and `FluentBadge` no longer appear in these files.
- [x] Any rule shared with [04](04-catalog-grid.md) or [06](06-basket-and-checkout.md) exists **once**,
      in `app.css`. (`.eshop-pages` and the checkout/order-detail vocabulary — `.eshop-checkout`,
      `.eshop-form-section`, `.eshop-checkout-line`, `.eshop-form-buttons` — were both promoted since
      04 and 06 had already landed; `CheckoutPage.razor.css` is now empty and was deleted.)
- [x] `dotnet build Tnosc.EShop.slnx` is clean.
