# Task 06 — Storefront catalogue

**Goal:** the first real screens — `/products` (a browsable card gallery with search, category filter and
paging) and `/products/{id:guid}` (detail). Both anonymous, both prerendered.

**Depends on:** [05](05-bff-proxy.md).

---

## Files to create — all in `.Client`

```
Features/Store/
├─ Home.razor                              (replace the task-03 placeholder)
└─ Catalog/
   ├─ Products.razor  Products.razor.cs
   ├─ ProductCard.razor
   ├─ ProductFilters.razor
   └─ ProductDetail.razor  ProductDetail.razor.cs
Features/Shared/
   └─ MoneyDisplay.razor                   (if not already done in task 03)
```

Logic goes in `.razor.cs` code-behind partial classes — analyzers behave predictably in real `.cs` files,
and [task 11](11-bunit-tests.md) gets a plain class to test.

---

## `/products` — a card gallery, not a data grid

A shopper browsing wants cards; a data grid is the wrong form here. (The grid comes back in
[task 09](09-admin-catalog.md), where it *is* the right form.)

- **Layout:** `FluentGrid` / `FluentGridItem` of `ProductCard`.
- **Search:** `<FluentTextInput TextInputType="TextInputType.Search" Immediate="true" ImmediateDelay="300" />`.
  There is **no `FluentSearch`** in v5, and **no `FluentTextField`** — see the correction table in
  [`00-overview.md`](00-overview.md). `ImmediateDelay` is the debounce; without it every keystroke is a
  request.
- **Category:** `FluentSelect<Category, Guid?>` with `Items`, `OptionText="@(c => c.Name)"`,
  `OptionValue="@(c => c.Id)"`. **Two type parameters** — `FluentSelect<Category>` is the v4 shape and
  will not compile.
- **Paging:** a `PaginationState` plus `<FluentPaginator State="@_pagination" />`.
- **Loading:** `FluentSkeleton` cards while fetching. **Empty:** an explicit empty message, not a blank
  page.

### Paging against a server-paged API

`PaginationState.CurrentPageIndex` is **0-based**; the API's `page` is **1-based**. Translate at exactly
one place, and after each fetch push the total back:

```csharp
await _pagination.SetTotalItemCountAsync(totalItemCount: result.Value.TotalCount);
```

Forgetting that leaves the paginator showing one page forever, no matter how many products exist.

### Filters live in the URL

Bind `Search`, `CategoryId` and `Page` to query-string parameters via `[SupplyParameterFromQuery]`, and
update them with `NavigationManager`. Three payoffs: a shared link reproduces the view, the back button
works, and **prerender is deterministic** — the server and the browser compute the same first render from
the same URL.

---

## `ProductCard`

`FluentCard` + `FluentText`, an out-of-stock `FluentBadge`, and a `FluentAnchorButton Href="/products/{id}"`.

Price formatting goes through `MoneyDisplay`:

```csharp
// CA1305 is a build error here — a culture-less ToString will not compile.
string formatted = amount.ToString(format: "N2", provider: CultureInfo.InvariantCulture);
```

Use `"N2"` plus the explicit currency code, **not** `"C"`. `"C"` depends on globalization data that
Release-mode WASM trimming may drop (see [task 12](12-polish-and-docs.md)), and it would render the
*browser's* currency symbol for an amount denominated in the server's.

Give the card a stable `data-testid` — [task 11](11-bunit-tests.md) asserts on our own markup, never on
Fluent's shadow DOM.

---

## `/products/{id:guid}`

`FluentGrid` two-column layout. Quantity via
`<FluentNumberInput TValue="int" Min="1" Max="@product.StockQuantity" StepButtons="true" />` —
**`FluentNumberInput<TValue>`**, not `FluentNumberField`.

"Add to basket" is a `FluentButton Appearance="ButtonAppearance.Primary" IconStart="@(new Icons.Regular.Size20.CartAdd())"`,
wrapped in `<AuthorizeView>`: authenticated users get the button, anonymous users get "Sign in to buy".
The basket call itself lands in [task 10](10-skeletons.md); wire the button to a no-op with a `TODO` until
then, or leave it disabled.

**404 goes inline, not to a toast.** A missing product is a property of the page, so render `ErrorPanel`
in place. Toasts are for the outcome of an action the user just took.

---

## Render mode

Both pages stay **prerendered** — they are anonymous, so there is nothing to leak and SEO benefits.

They will therefore fetch twice: once on the server during prerender, once when WASM boots. Accept it for
now; [task 12](12-polish-and-docs.md) removes the second fetch with `PersistentComponentState`. If the
flash is distracting while developing, note it and move on rather than reaching for `prerender: false` —
that would give up the SEO these two pages exist to get.

---

## Error handling

Every call returns `ApiResult<T>` and nothing throws. Branch on the result:

| Status | Presentation |
|---|---|
| 404 | inline `ErrorPanel` on the page |
| 5xx / transport | `ErrorPanel` with a retry button; include `problem.TraceId` if present |
| anything else | `ErrorCodeMessages.Humanize(problem)` — never show a raw error code to a shopper |

---

## Definition of done

- [ ] `/products` renders real products from Postgres through the BFF.
- [ ] Search debounces, filters by category, and pages correctly — including a **second** page.
- [ ] Filters survive a page reload and a back-button press (they are in the URL).
- [ ] `/products/{id}` renders a real product; a random GUID renders `ErrorPanel`, not a crash.
- [ ] The empty state appears for a search matching nothing.
- [ ] The grep gate from [task 03](03-shell-and-layouts.md) still returns zero hits.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.

**The real proof:** open dev tools, hard-refresh `/products`, and confirm the product markup is present
in the **initial HTML response** (prerender worked) *and* that the page becomes interactive afterwards
(WASM attached). Both halves matter — this is the first evidence that the one-client-two-hosts design in
[task 04](04-api-client-infrastructure.md) actually works.
