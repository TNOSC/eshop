# Task 02 — Brand assets

**Goal:** the images, icons and logo the redesigned shell and cards reference, plus the SKU → image
resolver that stands in for a product image field the API does not have.

**Depends on:** [01](01-design-tokens.md) (the resolver's placeholder uses the tokens).

---

## Why a resolver at all

`Tnosc.EShop.Client.Web.Contracts/Catalog/ProductSummary.cs` is
`(Guid Id, string Sku, string Name, decimal PriceAmount, string PriceCurrency, int StockQuantity,
string BrandName, string CategoryName)` — **there is no image field**, and `ProductDetail` has none
either. The reference storefront is image-first: the card is a photo with a name/price row under it.

Adding one would mean a value object on the `Product` aggregate, an EF configuration change, a read
model change, a DTO change and a migration across Domain / Application / Persistence / Api — a backend
change to fix a styling gap. Decided against: images are resolved **client-side from the SKU**, with a
neutral placeholder when nothing matches. If an image field is ever added server-side, only
`ProductImageResolver` changes.

---

## Files to create

```
Tnosc.EShop.Client.Web/wwwroot/
├─ images/
│  ├─ logo-header.svg          wordmark, currentColor
│  ├─ logo-footer.svg          same geometry, inverted fill
│  ├─ hero-home.webp           tall hero, home page
│  ├─ hero.webp                short hero, inner pages
│  └─ products/
│     ├─ 01.webp … 12.webp     the rotating product set
│     └─ placeholder.svg       neutral fallback
└─ icons/
   ├─ cart.svg  user.svg  filters.svg  arrow-left.svg

Tnosc.EShop.Client.Web.Client/
├─ Infrastructure/Catalog/ProductImageResolver.cs
└─ Features/Shared/ProductImage.razor (+ .razor.css)
```

> **Which project's `wwwroot`?** The **host** (`Tnosc.EShop.Client.Web/wwwroot/`), alongside the
> existing `app.css` and `favicon.png`. The `.Client` project's `wwwroot` holds only `appsettings*.json`
> and is not a natural home for static assets; the host's is already served at the site root, so
> `/images/…` and `/icons/…` resolve with no extra configuration.

## Assets

**Logo.** There is no Tnosc wordmark. Author a simple SVG one reading `TNOSC` with `eShop` set lighter
beside it, roughly `3:1`, with `fill="currentColor"` so a single file serves both header and footer —
the reference ships two near-identical files (`#000000` and `#f5f5f5`) purely because it uses `<img>`.
Inline the SVG in `StoreHeader` / `StoreFooter` instead of `<img src>` so `currentColor` actually
applies, and dark mode comes free. Keep `logo-footer.svg` out of the tree if the inline approach lands.

**Hero photos.** Two `.webp` files, wide and desaturated enough that ink-coloured text stays legible
over them. Home gets the tall one. Source them yourself — do **not** copy
`C:\Projects\eShop\src\WebApp\wwwroot\images\header*.webp`, which are AdventureWorks brand assets.

Both heroes need a scrim so the h1 stays readable regardless of photo: a `linear-gradient` overlay from
`--eshop-surface` at ~70% opacity to transparent, defined in `StoreHero.razor.css` in
[03](03-store-shell.md). Note this per-asset requirement when picking the images.

**Icons.** 24×24, `viewBox="0 0 24 24"`, stroked outlines at `stroke-width="1.5"`, `stroke="currentColor"`
— the reference hard-codes `stroke="black"` and then cannot tint them, which is why it re-inlines the
same two glyphs on the item page. Ours use `currentColor` from the start.

Only four are genuinely needed: `cart` (badge + basket summary header), `user` (user menu),
`filters` (filter sidebar header), `arrow-left` ("continue shopping", "back to basket"). Everywhere
else, prefer the Fluent icon set already referenced (`Icons.Regular.Size20.*`) rather than adding a
file.

**Product images.** Twelve neutral product photos at a consistent aspect ratio (4:3 or 1:1 — pick one
and keep it, the card relies on it). They are illustrative placeholders, so anything consistently lit
and background-neutral works.

## `ProductImageResolver`

Deterministic so a given SKU always shows the same picture across pages and reloads — a card and its
detail page disagreeing would read as a bug.

```csharp
namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Catalog;

/// <summary>
/// Resolves a stable illustrative image URL for a product from its SKU, standing in for a product
/// image the Catalog API does not expose. Deterministic, so a product shows the same image on the
/// card and on its detail page.
/// </summary>
public static class ProductImageResolver
{
    private const int ImageCount = 12;
    private const string Placeholder = "images/products/placeholder.svg";

    public static string For(string? sku) { … }
}
```

Rules the implementation must satisfy:

- `null`, empty or whitespace `sku` ⇒ `Placeholder`.
- Otherwise a **stable, non-randomised** hash. `string.GetHashCode()` is randomised per process and
  would give the server-prerendered and WASM renders different images, so the DOM would flip on
  hydration. Use an explicit `System.IO.Hashing.XxHash32`-free hand-rolled FNV-1a over the ordinal
  bytes, or sum the `char` values — anything deterministic and documented.
- Take `Math.Abs(hash % ImageCount) + 1`, format `D2`, return `images/products/{n}.webp`.
- **Relative, no leading slash**, matching the `ApiRoutes` convention already in the repo.
- `static` class with a `static` method, so no DI registration and nothing to inject — the
  no-`IConfiguration`-in-constructors rule is not in play.

## `ProductImage.razor`

A thin wrapper so every call site is identical and the fallback lives in one place.

```razor
<img class="eshop-product-image"
     alt="@Alt"
     src="@ProductImageResolver.For(sku: Sku)"
     loading="lazy" />
```

Parameters: `Sku` (required), `Alt` (required — the product name; never decorative, these carry
meaning). The scoped CSS sets `max-width: 100%; height: auto; display: block;` and **no border-radius**
— square corners are the design.

`loading="lazy"` matters on a 12-card grid. Do not add it to the product **detail** image, which is
above the fold.

---

## Definition of done

- [ ] Every file above exists and is served — browse to `/images/products/01.webp`, `/icons/cart.svg`
      and `/images/hero.webp` directly and get a 200.
- [ ] No asset is copied from `C:\Projects\eShop` — the logo is ours, the photos are ours.
- [ ] All four icons use `stroke="currentColor"`, no hard-coded stroke colour.
- [ ] `ProductImageResolver.For` is deterministic across processes: calling it for the same SKU in a
      unit test twice in separate runs yields the same path. A `null`/blank SKU yields the placeholder.
- [ ] `ProductImage` renders and its scoped CSS sets no radius.
- [ ] Nothing consumes them yet — this task adds assets only. `git diff` touches no existing component.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.
