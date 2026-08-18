// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.Lib.Web.Errors;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Components;

/// <summary>Changes an existing product's price. Does not require an idempotency key. Validation and
/// mapping are <see cref="IUpdateProductPriceService"/>'s responsibility.</summary>
public partial class UpdateProductPriceDialog : ComponentBase
{
    private static readonly string[] Currencies = ["USD", "EUR", "TND"];

    private readonly UpdateProductPriceViewModel _model = new();

    private EditContext _editContext = default!;
    private bool _isSubmitting;

    [CascadingParameter]
    public IDialogInstance Dialog { get; set; } = default!;

    [Inject]
    public IUpdateProductPriceService Service { get; set; } = null!;

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
    public decimal CurrentAmount { get; set; }

    [Parameter]
    [EditorRequired]
    public string CurrentCurrency { get; set; } = string.Empty;

    private string FormattedCurrentPrice =>
        string.Create(
            provider: CultureInfo.InvariantCulture,
            handler: $"{CurrentAmount:N2} {CurrentCurrency}");

    protected override void OnInitialized()
    {
        _model.Amount = CurrentAmount;
        _model.Currency = CurrentCurrency;
        _editContext = new EditContext(model: _model);
    }

    private async Task SubmitAsync()
    {
        _isSubmitting = true;

        try
        {
            ClientResult result = await Service.SubmitAsync(
                productId: ProductId,
                viewModel: _model,
                cancellationToken: CancellationToken.None);

            if (result.IsSuccess)
            {
                await Notifications.ShowSuccessToastAsync(title: "Price updated", message: Sku);
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
}
