# Task 09 — Admin catalogue

**Goal:** the admin product console — a server-paged `FluentDataGrid`, three dialogs (create, price,
stock), toasts, correct idempotency-key handling, and the bridge from code-keyed server validation to
form fields.

**Depends on:** [08](08-lock-down-proxy.md).

---

## Files to create — in `.Client`

```
Features/Admin/Catalog/
├─ AdminProducts.razor  AdminProducts.razor.cs
├─ CreateProductDialog.razor  CreateProductDialog.razor.cs  CreateProductModel.cs
├─ UpdateProductPriceDialog.razor(.cs)
└─ AdjustStockDialog.razor(.cs)
Infrastructure/Errors/
├─ ValidationCodeFieldMap.cs
└─ NotificationExtensions.cs
```

Plus the three write methods on `ICatalogApi`/`CatalogApi` deferred from
[task 04](04-api-client-infrastructure.md).

**Every page in `Features/Admin/` needs its own `@attribute [Authorize(Roles = "admin")]`** — the
`_Imports.razor` supplies `@layout` but **not** attributes.

---

## `AdminProducts` — the data grid

Here a grid *is* the right form (unlike the storefront gallery in
[task 06](06-storefront-catalog.md)).

```razor
<FluentDataGrid TGridItem="ProductSummary"
                ItemsProvider="@_productsProvider"
                Pagination="@_pagination"
                Loading="@_loading"
                ResizableColumns="true"
                ShowHover="true">
    <PropertyColumn Property="@(p => p.Sku)" Title="SKU" />
    <PropertyColumn Property="@(p => p.Name)" Title="Name" />
    <PropertyColumn Property="@(p => p.BrandName)" Title="Brand" />
    <PropertyColumn Property="@(p => p.CategoryName)" Title="Category" />
    <PropertyColumn Property="@(p => p.PriceAmount)" Title="Price" Format="N2"
                    Align="DataGridCellAlignment.End" />
    <PropertyColumn Property="@(p => p.StockQuantity)" Title="Stock"
                    Align="DataGridCellAlignment.End" />
    <TemplateColumn Title="Actions" Align="DataGridCellAlignment.End">
        <FluentButton Size="ButtonSize.Small"
                      IconStart="@(new Icons.Regular.Size16.Money())"
                      OnClick="@(() => OpenPriceDialogAsync(product: context))">Price</FluentButton>
        <FluentButton Size="ButtonSize.Small"
                      IconStart="@(new Icons.Regular.Size16.BoxMultiple())"
                      OnClick="@(() => OpenStockDialogAsync(product: context))">Stock</FluentButton>
    </TemplateColumn>
    <EmptyContent><FluentLabel>No products match the current filter.</FluentLabel></EmptyContent>
</FluentDataGrid>

<FluentPaginator State="@_pagination" />
```

`_productsProvider` is a `GridItemsProvider<ProductSummary>` translating `request.StartIndex` /
`request.Count` into the API's `page` / `pageSize`, then:

```csharp
return GridItemsProviderResult.From(
    items: result.Value.Items,
    totalItemCount: (int)result.Value.TotalCount);
```

`PagedResult<T>` maps onto `GridItemsProviderResult` exactly — that is why the server's shape is worth
mirroring faithfully in [task 02](02-contracts-project.md).

> **Do not set `Virtualize="true"` alongside `Pagination`.** Pick one. Paging is right here because the
> API is page-based.

After a dialog completes, refresh with `await _grid.RefreshDataAsync();` — not by mutating a local list,
which would drift from the server's ordering.

---

## Dialogs — the v5 shape

Dialog components are **plain Razor components** taking `IDialogInstance` as a cascading parameter.
`IDialogContentComponent<T>` is the **v4** pattern and does not exist in v5.

```razor
@code {
    [CascadingParameter] public IDialogInstance Dialog { get; set; } = default!;

    private async Task ConfirmAsync() => await Dialog.CloseAsync(result: DialogResult.Ok(value: createdId));
    private async Task CancelAsync()  => await Dialog.CloseAsync(result: DialogResult.Cancel());
}
```

Opened from the page:

```csharp
DialogOptions options = new()
{
    Header = { Title = "New product" },
    Size = DialogSize.Medium,
    Modal = true,
    PreventDismissOnOverlayClick = true,
};

IDialogInstance dialog = await DialogService.ShowDialogAsync<CreateProductDialog>(options: options);
DialogResult result = await dialog.Result;
if (!result.Cancelled) { await _grid.RefreshDataAsync(); }
```

> If `ShowDialogAsync` in rc.5 returns `Task<DialogResult>` directly rather than the instance, drop the
> second `await`. Check IntelliSense on first use rather than guessing.

Pass data in through `DialogOptions.Parameters` (a `Dictionary<string, object?>`), e.g. the product being
repriced.

### Form controls — v5 names

`CreateProductDialog` is an `EditForm` over `CreateProductModel` with a `DataAnnotationsValidator`:

| Field | Component |
|---|---|
| Sku, Name, Description | `FluentTextInput` — **not `FluentTextField`** |
| Price | `FluentNumberInput<decimal>` with `IsDecimal="true"` — **not `FluentNumberField`** |
| Stock | `FluentNumberInput<int>` |
| Currency | `FluentSelect<string, string>` |
| Category, Brand | `FluentSelect<Category, Guid>` — **two type parameters** |

Categories come from `ICatalogApi.GetCategoriesAsync`. There is no brands endpoint on the API today, so
either take the brand id as a `FluentTextInput` GUID field for now or hard-code the seeded ids — note
whichever you choose, and flag the missing endpoint.

---

## Idempotency-Key — the part that must be right

`POST /api/catalog/products` requires the header: missing → 400 `Idempotency.KeyMissing`, reused with a
different body → 409 `Idempotency.KeyReuse`.

**Not a `DelegatingHandler`.** A handler sits *inside* the `HttpClient` pipeline, so it would mint a fresh
key per send. But a **user-driven** retry — clicking "Save" again after a failure — is a fresh logical
send, and would therefore get a fresh key, creating a second product. The key must outlive the
`HttpRequestMessage`, which means it belongs to **component state**:

```csharp
private Guid _submissionKey = Guid.CreateVersion7();

private async Task SubmitAsync()
{
    ApiResult<Guid> result = await CatalogApi.CreateProductAsync(
        request: _model.ToRequest(),
        idempotencyKey: _submissionKey,
        cancellationToken: _cancellation.Token);

    // A response arrived — success OR business failure. The next attempt is a NEW logical request,
    // possibly with a different body, so rotate the key.
    _submissionKey = Guid.CreateVersion7();

    if (result.IsSuccess)
    {
        await Dialog.CloseAsync(result: DialogResult.Ok(value: result.Value));
        return;
    }

    // On a TRANSPORT failure (no response at all) do NOT rotate — retrying the identical body is
    // exactly the replay the server is built for.
}
```

The client method takes it explicitly:

```csharp
Task<ApiResult<Guid>> CreateProductAsync(
    CreateProductRequest request,
    Guid idempotencyKey,
    CancellationToken cancellationToken);
```

`Guid.CreateVersion7()` keys are monotonic and index-friendly on the server's idempotency table.

**This is load-bearing, not defensive.** `AddStandardResilienceHandler()` retries transient failures on
every method including `POST`, *below* the point where the header is set — so all Polly attempts carry
the same key. That is precisely why the API demands one, and why
[task 05](05-bff-proxy.md)'s deny-list header copy matters.

Also disable the submit button while a call is in flight (`FluentButton Loading`), as cheap
belt-and-braces on top.

---

## Server validation → form fields

`CustomResults.cs` keys the `errors` dictionary by **error code**, not field name
(`{ "Sku.InvalidFormat": ["…"] }`), so `<ValidationMessage For="…" />` cannot find it. Bridge with an
explicit map:

```csharp
internal static class ValidationCodeFieldMap
{
    private static readonly FrozenDictionary<string, string> CodeToField =
        new Dictionary<string, string>(comparer: StringComparer.Ordinal)
        {
            ["Sku.Required"] = nameof(CreateProductModel.Sku),
            ["Sku.InvalidFormat"] = nameof(CreateProductModel.Sku),
            ["Product.NameRequired"] = nameof(CreateProductModel.Name),
            ["Money.NegativeAmount"] = nameof(CreateProductModel.PriceAmount),
            ["Money.InvalidCurrency"] = nameof(CreateProductModel.PriceCurrency),
            ["Stock.NegativeQuantity"] = nameof(CreateProductModel.StockQuantity),
        }.ToFrozenDictionary(comparer: StringComparer.Ordinal);

    public static bool TryResolveField(string errorCode, [NotNullWhen(true)] out string? fieldName) =>
        CodeToField.TryGetValue(key: errorCode, value: out fieldName);
}
```

Applied through a `ValidationMessageStore` on the form's `EditContext`:

```csharp
private void ApplyServerValidation(ApiProblem problem)
{
    _messageStore.Clear();
    _unmappedMessages.Clear();

    foreach ((string code, string[] messages) in problem.Errors ?? EmptyErrors)
    {
        if (ValidationCodeFieldMap.TryResolveField(errorCode: code, fieldName: out string? field))
        {
            _messageStore.Add(fieldIdentifier: _editContext.Field(fieldName: field), messages: messages);
        }
        else
        {
            _unmappedMessages.AddRange(collection: messages);
        }
    }

    _editContext.NotifyValidationStateChanged();
}
```

> **The `_unmappedMessages` fallback is what makes this safe.** A new server error code that nobody has
> mapped yet lands in a `FluentMessageBar` at the top of the form instead of vanishing. The failure mode
> degrades; it never goes silent.

Client-side `DataAnnotationsValidator` still catches most mistakes before a request is sent — the map only
handles what the server rejects.

---

## Toasts

**`INotificationService` exists in rc.5** — the skill file wrongly says `IToastService` was removed with
no replacement. Inject it:

```csharp
await Notifications.ShowSuccessToastAsync(title: "Product created", subtitle: model.Sku);
```

Route failures by status:

| Status | Presentation |
|---|---|
| 400 with `Errors` | inline on the form + `FluentMessageBar` for unmapped codes |
| 401 | redirect to login (`forceLoad: true`, then `return;`) |
| 403 | `ShowErrorToastAsync("Not permitted")` — **never** redirect to login, that loops |
| 404 | inline `ErrorPanel` |
| 409 | `ShowWarningToastAsync` — conflicts are user-actionable (`Sku.AlreadyExists`, `Idempotency.KeyReuse`) |
| 5xx / transport | `ShowErrorToastAsync("Something went wrong", $"Reference {problem.TraceId}")` |

Always humanize through `ErrorCodeMessages` — never show a raw `Product.NotFound` to a user.

---

## Definition of done

- [ ] `/admin/products` pages against the **server** — page 2 issues a new request, and `TotalCount` is
      respected.
- [ ] Create, price-change and stock-adjust all round-trip and refresh the grid.
- [ ] Each admin page carries its own `[Authorize(Roles = "admin")]`; `customer@eshop.local` gets 403,
      not a login loop.
- [ ] **Idempotency proof:** submit the create dialog, then submit again from the same open dialog without
      editing. **Exactly one product exists**, and the second call replays the first response.
- [ ] A duplicate SKU produces an inline message on the Sku field, not a bare toast.
- [ ] An unmapped error code still reaches the message bar (force one by temporarily removing a map entry).
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.
