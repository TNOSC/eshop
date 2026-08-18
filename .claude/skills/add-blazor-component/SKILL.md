---
name: add-blazor-component
description: Scaffold a new Blazor client page or feature component in Tnosc.EShop — page composition, component markup, colocated ViewModel, and the I<Name>Service that owns the API client — following the strict MVVM split. Use when the user asks to add a page, screen, form, or feature component to the Blazor client.
argument-hint: <component description, e.g. "a page to list a customer's orders" or "an add-to-basket button with quantity">
---

# Add a Blazor Client Component

Scaffold a new page or feature component in `Tnosc.EShop.Client.Web.Client`, following the
**`Features/Store/Catalog`** slice, which is the reference implementation. Read
`.claude/rules/blazor-client-mvvm.md` before writing code — this skill turns that rule into steps,
it does not restate it.

Core split: a routable page only composes child components. A component with real behavior (a form,
a data-fetching container) owns a colocated `<Name>ViewModel` and an `I<Name>Service`; nothing else
touches an `IXxxApi` client, validates, or maps to a request contract.

## Workflow

1. **Classify what you're building.**
   - **Routable page** (`@page`) — composition root only.
   - **Behavior component** — fetches data, submits a form, or otherwise owns state.
   - **Presentational component** — renders `[Parameter]`s only, no state, no API call (e.g.
     `ProductCard`, `Pagination`, `MoneyDisplay`). Stop after the markup step — no ViewModel, no
     service. Adding either here is ceremony, not architecture.

2. **Place it**, mirroring `Features/Store/Catalog/`:
   ```
   Features/<Area>/<Context>/
       Pages/<Name>.razor(.cs)
       Components/<Name>.razor(.cs)
       ViewModels/<Name>ViewModel.cs
       Services/I<Name>Service.cs
       Services/<Name>Service.cs
   ```
   `<Area>` is `Store` or `Admin`; `<Context>` is the bounded context (`Catalog`, `Basket`, …).
   ViewModels and services are colocated per feature, never centralized.

3. **Write the page**, if you're adding one. Composition only — one outer wrapper (e.g.
   `<div class="eshop-page">`), child components, no loops/raw text/`[Inject]` of an `IXxxApi`. It
   may read route/query parameters and hand them to child `[Parameter]`s. Template, from
   `Pages/ProductsPage.razor`:

   ```razor
   @page "/products"
   @rendermode InteractiveAuto
   @using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Components

   <PageTitle>…</PageTitle>

   <div class="eshop-page">
       <ProductFilters Search="@Search" SearchChanged="@OnSearchChangedAsync" />
   </div>
   ```

4. **Write the component** — `<Name>.razor` (markup) + `<Name>.razor.cs` (partial class). The
   code-behind injects `I<Name>Service`, owns the `<Name>ViewModel` instance, and calls the service
   from lifecycle methods or event handlers — no mapping or validation logic of its own. Declare the
   load/submit cycle through `ComponentState` and wrap the body in `StatefulBoundary`, not a hand
   -rolled `bool _isLoading`:

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

   `ComponentState.Error` is set only by `StatefulBoundary`'s wrapped `ErrorBoundary`, when rendering
   throws — never by component code. A `ClientProblem` (404, validation, a mapped 500) is a normal
   outcome and stays `Content`, shown via `ErrorPanel`. A per-action busy flag on already-loaded
   content (`_isSavingProfile`, `_isPlacingOrder`) stays a plain `bool` — don't fold it into the enum.

5. **Write the ViewModel** — a plain POCO in `ViewModels/<Name>ViewModel.cs`. Add DataAnnotations
   (`[Required]`, `[Range]`, `[StringLength]`) to any user-editable property — they're opt-in per
   property, not an all-or-nothing ceremony tied to "this is a form component". A ViewModel with no
   user-editable properties at all (e.g. a list/filter container like `ProductsViewModel`) carries
   none — say so with a doc comment, don't add attributes to satisfy a checklist.

6. **Write the service** — `I<Name>Service.cs` / `<Name>Service.cs`, the only place touching an
   `IXxxApi` client for this component:

   ```csharp
   internal sealed class <Name>Service(IXxxApi xxxApi) : I<Name>Service
   {
       public async Task<ClientResult<TResponse>> SubmitAsync(<Name>ViewModel viewModel, CancellationToken cancellationToken)
       {
           ClientProblem? validation = ClientValidation.Validate(viewModel: viewModel);
           if (validation is not null)
           {
               return ClientResult<TResponse>.Failure(problem: validation);
           }

           // map viewModel -> request contract, call xxxApi, map response back
       }
   }
   ```

   A mutating method always calls `ClientValidation.Validate` first and short-circuits on failure —
   this is what makes the service unit-testable with a substituted `IXxxApi` and nothing else.

7. **Wire validation into the component, if it's a form.** Own an `EditContext`, a
   `ValidationMessageStore`, and a `List<string>` for unmapped messages. On submit: clear the message
   store, call `Service.SubmitAsync(...)`, and on failure call

   ```csharp
   ClientValidation.ApplyFieldErrors(problem, editContext, messageStore, unmappedMessages);
   ```

   — one code path regardless of whether the rejection was client-side (from step 6, keys are already
   property names) or server-side (keys are error codes, resolved through that component's own
   `ValidationCodeFieldMap`; an unmapped code falls into `unmappedMessages` instead of being dropped).
   See `Features/Admin/Catalog/Components/CreateProductDialog.razor.cs` for the full pattern,
   including the idempotency-key rotation.

8. **Register the service** in `Extensions/ClientServiceCollectionExtensions.cs`:

   ```csharp
   services.AddScoped<I<Name>Service, <Name>Service>();
   ```

   next to the other per-component services — not the `AddHttpClient<IXxxApi, XxxApi>(...)`
   registrations, which are per-context (one per `IXxxApi`), not per-component.

9. **Extract shared markup.** A block that would duplicate one already in another page or component
   (pagination, a delivery-address panel, a list/empty-state shape) becomes a shared presentational
   component under `Features/Shared/` instead of being copy-pasted — see `Pagination.razor`,
   `DeliveryAddress.razor`.

10. **Write a service test** —
    `tests/client/Tnosc.EShop.Client.Web.Tests.Unit/Features/<Area>/<Context>/<Name>ServiceTests.cs`,
    substituting `IXxxApi` with NSubstitute. No bUnit or DOM rendering is needed because the service
    holds all the logic — see `ProductsServiceTests.cs`, `CreateProductServiceTests.cs`.

11. **Verify.** `dotnet build Tnosc.EShop.slnx` (warnings are errors), then
    `dotnet test tests/client/Tnosc.EShop.Client.Web.Tests.Unit`.

If the component needs an API endpoint that doesn't exist yet, that's server-side work — use the
`add-feature` skill first.

## Non-negotiable conventions

- No page or component `[Inject]`s an `IXxxApi` client directly, except the one `<Name>Service` that
  owns it (`.claude/rules/blazor-client-mvvm.md`).
- `ComponentState.Error` is never set by component code — only `StatefulBoundary`'s `ErrorBoundary`
  sets it; a `ClientProblem` failure still renders as `Content` via `ErrorPanel`.
- Presentational components (no state, no API call of their own) get no ViewModel and no service —
  don't add either as ceremony.
- Every call with 3+ arguments is one per line, every argument named at every call site
  (`.claude/rules/code-style.md`).
- `IConfiguration`/`IOptions<T>` never appear in a component or service constructor
  (`.claude/rules/configuration-options.md`) — client-side settings follow the same rule as the
  server.

## Naming reference

| Artifact | Pattern | Lives in |
|---|---|---|
| Page | `<Name>.razor` / `.razor.cs` | `Features/<Area>/<Context>/Pages/` |
| Component | `<Name>.razor` / `.razor.cs` | `Features/<Area>/<Context>/Components/` |
| ViewModel | `<Name>ViewModel.cs` | `Features/<Area>/<Context>/ViewModels/` |
| Service | `I<Name>Service.cs` / `<Name>Service.cs` | `Features/<Area>/<Context>/Services/` |
| Service test | `<Name>ServiceTests.cs` | `tests/client/Tnosc.EShop.Client.Web.Tests.Unit/Features/<Area>/<Context>/` |
