# Task 10 — Skeletons

**Goal:** the remaining screens against real endpoints — basket, checkout, order history, admin customers.
Thinner UI than tasks 06 and 09, but no mocks: every endpoint listed here exists and works today.

**Depends on:** [09](09-admin-catalog.md). Can be done in any order alongside [11](11-bunit-tests.md) and
[12](12-polish-and-docs.md).

---

## Files to create — in `.Client`

```
Infrastructure/Api/
├─ IBasketApi.cs    BasketApi.cs
├─ IOrderingApi.cs  OrderingApi.cs
└─ IIdentityApi.cs  IdentityApi.cs
Features/Store/
├─ Basket/   BasketPage.razor(.cs)   BasketLineRow.razor
├─ Checkout/ CheckoutPage.razor(.cs) OrderPlaced.razor
└─ Orders/   MyOrders.razor(.cs)     OrderDetail.razor(.cs)
Features/Admin/Identity/
├─ AdminCustomers.razor(.cs)
└─ AdminCustomerDetail.razor(.cs)
```

Register the three new clients in `AddEShopApiClients` — one line each, both hosts get them automatically.

---

## Render mode

**Every page in this task is authenticated, so every one gets `prerender: false`:**

```razor
@rendermode @(new InteractiveAutoRenderMode(prerender: false))
@attribute [Authorize]
```

Two reasons, both real: it removes the double-fetch, and it avoids the "prerender renders the anonymous
state, then the page flips to authenticated" flash that looks like a bug to anyone watching.

---

## Basket — `/basket`

| Endpoint | Note |
|---|---|
| `GET api/basket` | **never 404s** — an empty basket is a `BasketDto` with no items, so there is no not-found path to write |
| `POST api/basket/items` | returns **200 with the whole `BasketDto`**, not 201 |
| `PUT api/basket/items/{itemId}` | returns 200 with the whole `BasketDto` |
| `DELETE api/basket/items/{itemId}` | 204 |
| `DELETE api/basket` | 204 |

Because add and change return the **entire basket**, never patch local state — assign the returned
`Basket` and re-render. That keeps the client's totals identical to the server's, which matters when
pricing rules live in the domain.

- `FluentDataGrid<BasketItem>` with `Items="@_basket.Items.AsQueryable()"` — client-side is right here,
  a basket is small.
- A `TemplateColumn` holding `FluentNumberInput<int>` bound to quantity → `ChangeQuantityAsync`.
- Delete behind `DialogService.ShowConfirmationAsync`.
- Empty basket → an empty state with a link to `/products`, not a blank grid.

Now wire up the "Add to basket" button left as a TODO in [task 06](06-storefront-catalog.md), and the
`BasketBadge` in `StoreLayout` from [task 03](03-shell-and-layouts.md).

---

## Checkout — `/checkout`

`POST /api/orders` takes **no body** and requires an **`Idempotency-Key`** header. Same discipline as
[task 09](09-admin-catalog.md), and this is the case where getting it wrong is most expensive: a
duplicate order is a duplicate charge.

```csharp
private Guid _submissionKey = Guid.CreateVersion7();   // minted ONCE when the page loads
```

Rotate it only after a response arrives. On a transport failure keep it, so a retry replays rather than
places a second order. Disable the button while in flight.

The page reads `GET api/basket` and `GET api/identity/customers/me` for the shipping address, shows a
read-only summary, then places the order and navigates to `/orders/{id}` — `forceLoad` is not needed here
(it is an in-app navigation), but the `return;` after `NavigateTo` still is.

A 409 on checkout means the basket changed underneath — refresh it and explain, rather than showing a raw
conflict code.

---

## Orders — `/orders`, `/orders/{id:guid}`

`GET api/orders` is paged; `GET api/orders/{id}` returns the full `Order` with lines.

`FluentDataGrid` + `GridItemsProvider` (same pattern as `AdminProducts`), a `FluentBadge` for `Status`.
`Status` is a plain wire `string` — render it as-is, do not map it to a client enum, or an unrecognised
server value becomes a deserialization crash instead of an unfamiliar label.

`POST /orders/{id}/confirm` and `/cancel` are available to the customer; `/ship` needs `ordering:ship`
(admin only). Confirm and cancel belong on `OrderDetail` behind `ShowConfirmationAsync`.

---

## Admin customers — `/admin/customers`, `/admin/customers/{id:guid}`

`@attribute [Authorize(Roles = "admin")]` on **both** — `_Imports.razor` gives the layout, not the
attribute.

| Endpoint | Permission |
|---|---|
| `GET api/identity/customers` (paged) | `identity:read` |
| `GET api/identity/customers/{id}` | `identity:read` |
| `PUT api/identity/customers/{id}/profile` | `identity:write` |
| `POST/PUT/DELETE .../{id}/addresses[/{addressId}]` | `identity:write` |
| `POST api/identity/customers/{id}/deactivate` | `identity:write` |

List: `FluentDataGrid` + `GridItemsProvider`, columns for Email / FirstName / LastName, a `FluentBadge`
for `IsActive`, a row link to the detail page.

Detail: profile fields, the address list with add/edit/delete/set-default, and a "Deactivate" button
behind `ShowConfirmationAsync`. Deactivate can return **409** — surface it as a warning toast, not an
error.

> **Note the `me` vs admin split.** `me` endpoints resolve the caller from `IUserContext` and accept no
> identifier, so a customer structurally cannot address another customer's profile. The admin endpoints
> take the id in the route and are permission-gated instead. Never build a screen that sends a customer id
> to a `me` endpoint — see [`.claude/rules/authorization.md`](../.claude/rules/authorization.md).

---

## Not in scope

**Payment.** The four `api/payments` endpoints all require `payment:read` / `payment:write`, and the
customer journey has no payment step wired to the UI yet. Leave it; note it as follow-up work.

---

## Definition of done

- [ ] Add a product to the basket from `/products/{id}`, see it in `/basket` and in the header badge.
- [ ] Change a quantity and remove a line; totals track the server, not local arithmetic.
- [ ] `/checkout` places a real order and lands on the confirmation page.
- [ ] **Double-click "Place order":** exactly one order exists.
- [ ] `/orders` lists it; `/orders/{id}` shows the lines.
- [ ] `/admin/customers` pages; the detail page loads; deactivate works and a repeat gives a 409 warning.
- [ ] `customer@eshop.local` gets 403 on `/admin/customers`, not a login loop.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.
