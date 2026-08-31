---
description: "Blazor pages compose only; components own a colocated ViewModel and service; the shared ClientValidation helper"
applyTo: "src/client/**"
---

# Rule — Blazor client: pages compose, components own a ViewModel + service

**A routable page (`@page`) contains no business markup — only child components.** A component with
real behavior (a form, a data-fetching container) owns a `<Name>ViewModel` and injects an
`I<Name>Service`; nothing outside that service touches an `IXxxApi` client, validates, or maps to a
request contract.

## Why

Before this rule, every page's `.razor.cs` injected `IXxxApi` clients directly and mixed
fetch/paging/validation/mapping into the same partial class as the component's lifecycle methods
(see `Products.razor.cs` and `CreateProductDialog.razor.cs` prior to this rule). Two consequences:

- **Nothing in `Features/` was unit-testable without bUnit rendering.** A `ComponentBase` holding
  private fields and calling `ICatalogApi` inline can only be exercised by rendering it.
- **Markup and orchestration duplicated together.** `Products.razor` and `MyOrders.razor` each
  hand-rolled an identical pagination block; `CheckoutPage.razor` and `OrderDetail.razor` each
  hand-rolled a near-identical delivery-address block. Neither the markup nor the loading logic
  around it was shared, because nothing forced the "block with its own concern" out of the page.

Separating page (composition), component (markup), ViewModel (state + `[Required]`/`[Range]`/…) and
service (validate → map → call → map back) means a service is testable with a substituted `IXxxApi`
and nothing else — no DOM, no `EditContext`, no rendering.

## How

**Page (`@page`-attributed `.razor`)**
- Composition root only: one outer structural element is allowed for CSS layout (e.g.
  `<div class="eshop-page">`), nothing else raw. No loops, no `<p>`/`<a>`/`<h2>` text, no
  conditionals beyond switching between child/empty-state components, no `[Inject]` of an `IXxxApi`
  client.
- A page's code-behind may read route/query parameters and hand them to child `[Parameter]`s. It
  does not call an API directly.

**Component with behavior (non-page, or a page acting as its own container)**
- `<Name>.razor` — markup, binds to `<Name>ViewModel`.
- `<Name>.razor.cs` — thin: injects `I<Name>Service`, owns the `<Name>ViewModel` instance, calls the
  service from lifecycle methods/event handlers, no mapping or validation logic of its own.
- `<Name>ViewModel.cs` — plain POCO, colocated with the component (not centralized). DataAnnotations
  attributes (`[Required]`, `[Range]`, `[StringLength]`) go here for anything user-editable. A
  ViewModel with no user input (e.g. a list/filter container) carries none — attributes are opt-in,
  not mandatory ceremony.
- `I<Name>Service` / `<Name>Service` — the only place touching an `IXxxApi` client for that
  component. Validates the ViewModel, maps it to a request contract, calls the API client, maps the
  response back. Registered scoped in `ClientServiceCollectionExtensions.cs`, alongside the typed
  API clients it wraps.

**Presentational components take no `[Parameter]` and no service — but never a Contracts type
either.** `ProductCard`, `BasketLineRow`, `OrderStatusPill`, `MoneyDisplay`, `PageHeader`,
`Pagination`, `DeliveryAddress`, etc. still get no ViewModel and no service of their own — adding
either to a component with no state or API call of its own is pure ceremony. What changes: none of
their `[Parameter]`s may be a type from `Tnosc.EShop.Client.Web.Contracts`, including read-only
display data. A component whose parameters are already primitives (`DeliveryAddress`'s
`Street`/`City`/`PostalCode`/`Country`) needs nothing new. A component whose parameters currently take
a DTO (`ProductCard.Product`, `BasketLineRow.Item`, `ProductFilters.Categories`) takes a colocated
display ViewModel instead — see "No DTO past the service" below. `StatefulBoundary` itself (below) is
exempt for the same reason — it is a shared framework component, not a per-feature one.

### State — one enum, three members, wrapped in `StatefulBoundary`

Every component with real behavior declares its lifecycle through
`Tnosc.Lib.Web.Components.Shared.ComponentState` (`Loading`, `Error`, `Content`) and renders its body
through `StatefulBoundary`, instead of a hand-rolled `bool _isLoading` plus an `if`/`else if`/`else`
chain in the markup:

```csharp
private ComponentState _state = ComponentState.Loading;
private ClientProblem? _problem;

protected override async Task OnInitializedAsync()
{
    ClientResult<Order> result = await Service.LoadAsync();
    _problem = result.IsSuccess ? null : result.Problem;
    _state = ComponentState.Content;   // set on BOTH success and business failure
}
```

```razor
<StatefulBoundary State="_state">
    @if (_problem is not null)
    {
        <ErrorPanel Message="@ErrorCodeMessages.Humanize(_problem)" />
    }
    else
    {
        <!-- real content -->
    }
</StatefulBoundary>
```

**`ComponentState.Error` means the component crashed**, not that a call failed. A `ClientProblem`
returned from a service (404, validation, a 500 mapped by `ApiResponseReader`) is a normal outcome of
a load — it is `Content`, shown via `ErrorPanel`, exactly as before this rule. `Error` is set
internally by `StatefulBoundary`'s wrapped `ErrorBoundary` only when rendering `ChildContent` throws
an unhandled exception; a component never sets `_state = ComponentState.Error` itself. Only the
initial load/submit cycle gets this treatment — a per-action busy flag on already-loaded content
(`_isSavingProfile`, `_isPlacingOrder`, `_isAddingToBasket`) stays a plain `bool`; folding it into the
enum would misrepresent what `Error` means for a failure the form already recovers from inline via
`ClientValidation.ApplyFieldErrors`.

### Validation — one shared helper, two halves

`Infrastructure/Validation/ClientValidation.cs` is used by every form component; it is not
reimplemented per component.

1. `ApiProblem? Validate<TViewModel>(TViewModel viewModel)` runs
   `System.ComponentModel.DataAnnotations.Validator.TryValidateObject` and, on failure, packs the
   results into an `ApiProblem` (`Status: 400`, `ErrorCode: ClientValidation.ValidationErrorCode`,
   `Errors` keyed by the ViewModel's own property names). A service's submit method calls this first
   and short-circuits with `ApiResult<T>.Failure(problem)` on failure — this is what makes the
   service callable and testable without an `EditContext`.
2. `void ApplyFieldErrors(ApiProblem problem, EditContext editContext, ValidationMessageStore messageStore, ICollection<string> unmappedMessages)`
   merges an `ApiProblem`'s `Errors` back onto the form. When `problem.ErrorCode` is
   `ClientValidation.ValidationErrorCode`, the dictionary's keys are already property names (from
   part 1) and are used directly. Otherwise the keys are **server error codes** and are resolved to
   a field through that component's own code-to-field map (see `ValidationCodeFieldMap` for the
   `CreateProductViewModel` example) — a code with no entry is not a bug: it falls into
   `unmappedMessages` for a top-level message bar instead of being silently dropped.

Because both paths converge on the same `ApiProblem` shape, a component's failure handling is one
code path regardless of whether the rejection was client-side (never left the browser) or
server-side (a 400/409 response): clear the message store → call `Service.SubmitAsync(...)` → on
failure, `ClientValidation.ApplyFieldErrors(...)`.

### No DTO past the service — read-only display data is mapped too

A type from `Tnosc.EShop.Client.Web.Contracts` (`Product`, `ProductSummary`, `Category`, `Order`,
`OrderLine`, `OrderSummary`, `Basket`, `BasketItem`, `Customer`, `CustomerAddress`,
`CustomerSummary`, …) never appears in a page's private fields, a component's `[Parameter]`s, or a
`GridItemsProvider<T>`/`FluentDataGrid<T>` type argument — a read-only display value is mapped
exactly as a request contract already is, not just a form submission. The `I<Name>Service` that owns
the `IXxxApi` client for that page/component maps every DTO it returns into a colocated ViewModel
before returning it; nothing downstream of the service ever sees the DTO shape.

- A page's private display field (`ProductsPage._products`, `OrderDetailPage._order`,
  `CheckoutPage._basket`, …) holds the mapped ViewModel type.
- A presentational component's `[Parameter]` (`ProductCard.Product`, `BasketLineRow.Item`,
  `ProductFilters.Categories`) takes the mapped ViewModel type. When the same DTO shape reaches more
  than one component from the same page (`ProductsPage` → `ProductCard`), it is **one** ViewModel
  colocated in that feature's `ViewModels/` folder, shared by both — not duplicated per component.
- A `GridItemsProvider<T>`/`FluentDataGrid<T>`, server-paged or in-memory (`AdminProductsPage`,
  `AdminCustomersPage`, `AdminCustomerDetailPage`'s address grid), is typed to a colocated
  row/list-item ViewModel, never the DTO; the service performs the DTO → ViewModel mapping in the same
  call that already builds the `PagedResult<T>`/list.
- A DTO's nested collection (`Order.Lines`, `Basket.Items`, `Customer.Addresses`) gets its own nested
  ViewModel, colocated with the parent, mapped in the same service method.
- Name the ViewModel after the DTO it replaces, not the component consuming it
  (`ProductSummaryViewModel`, not `ProductCardViewModel`) — several DTOs already flow through more
  than one component in the same slice, and naming after the first consumer misleads the next. Use
  `<Dto>RowViewModel` for a grid-only shape (`ProductRowViewModel`, `CustomerRowViewModel`). A
  display ViewModel that would collide with an existing user-editable ViewModel for the same DTO
  (`AdminCustomerDetailPage`'s add-address form already owns `CustomerAddressViewModel`) is
  disambiguated explicitly (`CustomerAddressListItemViewModel`).
- This does not touch a user-editable ViewModel already mapped to a request contract on submit
  (`CreateProductViewModel`, `CustomerProfileViewModel`, `CustomerAddressViewModel`) — those never
  held a DTO to begin with.

### Naming and placement

`<Name>ViewModel.cs`, `I<Name>Service.cs`/`<Name>Service.cs` — colocated with `<Name>.razor` in the
same `Features/**` folder, not centralized. Mirrors how `configuration-options.instructions.md` colocates
`<Feature>Options` with the `AddXxx` method that owns it rather than centralizing settings classes.

## Checklist

- [ ] A page contains at most one structural wrapper element; everything else is a child component.
- [ ] No page or component `[Inject]`s an `IXxxApi` client directly, except the one `<Name>Service`
      that owns it.
- [ ] Every user-editable ViewModel carries DataAnnotations attributes for its constraints.
- [ ] A service's mutating method calls `ClientValidation.Validate` before calling the API.
- [ ] A form's failure handling calls `ClientValidation.ApplyFieldErrors` for both client- and
      server-side rejections, not two separate code paths.
- [ ] A markup block duplicated across two pages is extracted into a shared presentational component
      instead of copy-pasted (see `Pagination.razor`, `DeliveryAddress.razor`).
- [ ] A component with a load/submit cycle declares `ComponentState` and wraps its body in
      `StatefulBoundary`, instead of a private `bool _isLoading`.
- [ ] A `ClientProblem` failure still renders as `Content` (via `ErrorPanel`); `_state` is never set
      to `Error` by component code — only `StatefulBoundary`'s wrapped `ErrorBoundary` does that.
- [ ] No type from `Tnosc.EShop.Client.Web.Contracts` appears in a page's fields, a component's
      `[Parameter]`s, or a `GridItemsProvider<T>`/`FluentDataGrid<T>` type argument — the owning
      service maps every DTO to a colocated ViewModel before returning it.
