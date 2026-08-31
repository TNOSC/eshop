// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.Lib.Web.Errors;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Components;

/// <summary>Uploads or removes an existing product's image. Validation and mapping are
/// <see cref="IProductImageService"/>'s responsibility — this class only wires the picker to it.</summary>
public partial class ProductImageDialog : ComponentBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private bool _isUploading;
    private bool _isRemoving;
    private string? _currentImageUrl;

    [CascadingParameter]
    public IDialogInstance Dialog { get; set; } = default!;

    [Inject]
    public IProductImageService Service { get; set; } = null!;

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
    public string? CurrentImageUrl { get; set; }

    protected override void OnInitialized() => _currentImageUrl = CurrentImageUrl;

    private async Task OnFileUploadedAsync(FluentInputFileEventArgs file)
    {
        if (file.Stream is null)
        {
            return;
        }

        _isUploading = true;

        try
        {
            await using Stream stream = file.Stream;
            ClientResult result = await Service.UploadAsync(
                productId: ProductId,
                content: stream,
                fileName: file.Name,
                contentType: file.ContentType,
                cancellationToken: CancellationToken.None);

            if (result.IsSuccess)
            {
                await Notifications.ShowSuccessToastAsync(title: "Image uploaded", message: Sku);
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
            _isUploading = false;
        }
    }

    private async Task OnFileErrorAsync(FluentInputFileErrorEventArgs error) =>
        await Notifications.ShowErrorToastAsync(title: "Upload rejected", message: error.ErrorMessage);

    private async Task RemoveImageAsync()
    {
        _isRemoving = true;

        try
        {
            ClientResult result = await Service.RemoveAsync(productId: ProductId, cancellationToken: CancellationToken.None);

            if (result.IsSuccess)
            {
                await Notifications.ShowSuccessToastAsync(title: "Image removed", message: Sku);
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
            _isRemoving = false;
        }
    }

    private async Task CancelAsync() => await Dialog.CancelAsync();
}
