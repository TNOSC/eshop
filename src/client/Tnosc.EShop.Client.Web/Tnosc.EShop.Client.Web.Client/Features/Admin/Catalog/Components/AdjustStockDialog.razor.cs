// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Errors;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Components;

/// <summary>Adjusts an existing product's stock level by a signed delta. Does not require an idempotency key.</summary>
public partial class AdjustStockDialog : ComponentBase
{
    private readonly FormModel _model = new();

    private EditContext _editContext = default!;
    private bool _isSubmitting;

    [CascadingParameter]
    public IDialogInstance Dialog { get; set; } = default!;

    [Inject]
    public ICatalogApi CatalogApi { get; set; } = null!;

    [Inject]
    public INotificationService Notifications { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public Guid ProductId { get; set; }

    [Parameter]
    [EditorRequired]
    public string Sku { get; set; } = string.Empty;

    [Parameter]
    [EditorRequired]
    public int CurrentStock { get; set; }

    protected override void OnInitialized() => _editContext = new EditContext(model: _model);

    private async Task SubmitAsync()
    {
        _isSubmitting = true;

        try
        {
            ClientResult result = await CatalogApi.AdjustStockAsync(
                productId: ProductId,
                request: new AdjustStockRequest(Delta: _model.Delta),
                cancellationToken: CancellationToken.None);

            if (result.IsSuccess)
            {
                await Notifications.ShowSuccessToastAsync(title: "Stock adjusted", message: Sku);
                await Dialog.CloseAsync(result: true);
                return;
            }

            await NotificationExtensions.NotifyFailureAsync(
                problem: result.Problem!,
                notifications: Notifications,
                navigation: Navigation,
                humanize: ErrorCodeMessages.Humanize);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task CancelAsync() => await Dialog.CancelAsync();

    private sealed class FormModel : IValidatableObject
    {
        public int Delta { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Delta == 0)
            {
                yield return new ValidationResult(
                    errorMessage: "A non-zero stock adjustment is required.",
                    memberNames: [nameof(Delta)]);
            }
        }
    }
}
