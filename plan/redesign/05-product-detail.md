# Task 05 — Product detail

**Goal:** `/products/{id}` as the reference's item page — a two-column body with the image left and the
description column right, and the price sitting on the same row as the add-to-basket button.

**Depends on:** [03](03-store-shell.md). Independent of [04](04-catalog-grid.md).

---

## Files to edit

`Features/Store/Catalog/ProductDetail.razor` (+ its `.razor.cs`, only if the loading branch moves).
**Created:** `ProductDetail.razor.css`.

---

## What changes

Today the page is a `FluentGrid` with two `FluentGridItem Xs="12" Md="6"` halves: **text left, controls
right**, and no image anywhere. The reference is **image left, everything else right**, with the price
and the buy button on one row.

The hero already carries the name and brand from [03](03-store-shell.md), so the `FluentText As="TextTag.H1"`
inside the body must go — otherwise the page renders two `h1`s. Keep the SKU line; it moves under the
description.

```razor
<div class="eshop-page eshop-item">
    <ProductImage Sku="@_product.Sku" Alt="@_product.Name" />

    <div class="eshop-item-description">
        @if (!string.IsNullOrWhiteSpace(_product.Description))
        {
            <p>@_product.Description</p>
        }
        <p>Brand: <strong>@_product.BrandName</strong></p>
        <p class="eshop-item-sku">@_product.Sku</p>

        @if (_product.StockQuantity <= 0)
        {
            <p class="eshop-pill" style="--eshop-pill-color: var(--eshop-status-bad);">Out of stock</p>
        }

        <AuthorizeView>
            <Authorized>
                <FluentNumberInput TValue="int"
                                   Label="Quantity"
                                   Min="1"
                                   Max="@_product.StockQuantity"
                                   Value="@_quantity"
                                   ValueChanged="@(quantity => { _quantity = quantity; })"
                                   StepButtons="NumberInputStepVisibility.Visible"
                                   Disabled="@(_product.StockQuantity <= 0)" />

                <div class="eshop-item-buy">
                    <span class="eshop-item-price">
                        <MoneyDisplay Amount="@_product.PriceAmount" Currency="@_product.PriceCurrency" />
                    </span>
                    <FluentButton Appearance="ButtonAppearance.Primary"
                                  IconStart="@(new Icons.Regular.Size20.Cart())"
                                  Loading="@_isAddingToBasket"
                                  Disabled="@(_product.StockQuantity <= 0 || _isAddingToBasket)"
                                  OnClick="@AddToBasketAsync">
                        Add to shopping bag
                    </FluentButton>
                </div>
            </Authorized>
            <NotAuthorized>
                <div class="eshop-item-buy">
                    <span class="eshop-item-price">
                        <MoneyDisplay Amount="@_product.PriceAmount" Currency="@_product.PriceCurrency" />
                    </span>
                    <a class="eshop-button eshop-button-primary" href="bff/login">Sign in to buy</a>
                </div>
            </NotAuthorized>
        </AuthorizeView>
    </div>
</div>
```

Two behavioural details to preserve exactly:

- **The quantity input is inside `<Authorized>`.** It is today (it is not — check), and it should be:
  an anonymous visitor cannot add to basket, so a stepper they cannot act on is noise. If moving it
  changes `_quantity`'s lifecycle, leave the field alone — only its render position moves.
- **`bff/login` is a relative href with no leading slash**, matching the existing code and the
  `ApiRoutes` convention. `FluentAnchorButton Href="bff/login"` becoming a plain `<a>` is fine here
  because the reference styles this as a text button; keep the href string identical.

```css
.eshop-item {
    display: flex;
    align-items: flex-start;
    gap: 4rem;
    line-height: 1.7rem;
}

.eshop-item > img,
.eshop-item ::deep .eshop-product-image {
    width: 25rem;
    max-width: 50%;
}

.eshop-item-description { max-width: 30rem; }

.eshop-item-sku {
    color: var(--eshop-muted);
    font-size: var(--eshop-body-size);
}

.eshop-item-buy {
    display: flex;
    align-items: center;
    gap: 1.2rem;
}

.eshop-item-price {
    font-size: var(--eshop-price-size);
    font-weight: var(--eshop-weight-semibold);
}

@media only screen and (max-width: 1024px) {
    .eshop-item { flex-direction: column; gap: 2rem; }
    .eshop-item > img,
    .eshop-item ::deep .eshop-product-image { width: 100%; max-width: 100%; }
    .eshop-item-description { max-width: none; }
}
```

Note the `::deep` is **anchored to `.eshop-item`** — `ProductImage` is a child component, so its `<img>`
carries the child's scope attribute, not this component's. A bare `::deep img` would be the leak the
reference has.

> The reference uses an asymmetric gutter here (`padding: 0 5rem 0 10rem`) to let the image sit closer
> to the right edge. We do not reproduce that — `.eshop-page` gives a symmetric gutter, and the
> asymmetry reads as a mistake at anything but the reference's exact image size.

The reference's add-to-cart button is the only rounded button in its entire app (`border-radius: .25rem`).
Ours is a `FluentButton`, which brings its own radius — leave it. Do not add a rule to square it off;
Fluent controls keep their own shape, only our containers are square.

## Loading, error and post-add states

- Keep `ErrorPanel` + `ErrorCodeMessages.Humanize(_problem)` unchanged.
- Replace the single `FluentSkeleton Height="320px"` with two — an image-shaped block and a text-shaped
  one in the same flex row — so the layout does not jump.
- Keep the existing add-to-basket toast/notification path exactly as-is. The reference shows an
  "N in shopping bag" line after adding; if `_isAddingToBasket`'s completion path already surfaces
  feedback, do not add a second channel.

---

## Definition of done

- [ ] The page renders **one** `h1` (the hero's) — the body's `FluentText As="TextTag.H1"` is gone.
- [ ] Image left at `25rem`/50%, description right at `30rem`, stacked below 1024px.
- [ ] Price and buy button share a row; the price is `1.6rem/600`.
- [ ] Anonymous visitors see the price and a "Sign in to buy" link, no quantity stepper.
- [ ] Adding to basket still works and the header count still increments (regression check on
      [03](03-store-shell.md)).
- [ ] Out-of-stock renders as an outlined pill, not `FluentBadge`.
- [ ] No bare `::deep`, no hard-coded colour.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.
