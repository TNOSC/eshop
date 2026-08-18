// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Components;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;
using Tnosc.Lib.Web.Components.Shared;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Pages;

/// <summary>The admin product console: a server-paged grid plus create/price/stock dialogs. Fetching
/// is <see cref="IAdminProductsService"/>'s responsibility.</summary>
public partial class AdminProductsPage : ComponentBase
{
    private const int PageSize = 20;

    private readonly PaginationState _pagination = new() { ItemsPerPage = PageSize };

    private FluentDataGrid<ProductRowViewModel> _grid = default!;
    private GridItemsProvider<ProductRowViewModel> _productsProvider = default!;
    // The grid mounts and shows its own built-in loading spinner immediately, so there is no
    // separate "not yet mounted" state to gate on here — ComponentState only ever reaches Content
    // (or Error, if rendering the grid itself throws).
    private readonly ComponentState _state = ComponentState.Content;
    private ClientProblem? _problem;

    [Inject]
    public IAdminProductsService Service { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override void OnInitialized() => _productsProvider = ProvideProductsAsync;

    private async ValueTask<GridItemsProviderResult<ProductRowViewModel>> ProvideProductsAsync(
        GridItemsProviderRequest<ProductRowViewModel> request)
    {
        int page = (request.StartIndex / PageSize) + 1;

        ClientResult<PagedResult<ProductRowViewModel>> result = await Service.SearchAsync(
            page: page,
            pageSize: PageSize,
            cancellationToken: request.CancellationToken);

        if (!result.IsSuccess)
        {
            _problem = result.Problem;
            return GridItemsProviderResult.From<ProductRowViewModel>(items: [], totalItemCount: 0);
        }

        _problem = null;
        return GridItemsProviderResult.From<ProductRowViewModel>(
            items: [.. result.Value.Items],
            totalItemCount: (int)result.Value.TotalCount);
    }

    private async Task OpenCreateDialogAsync()
    {
        DialogOptions options = new()
        {
            Header = { Title = "New product" },
            Size = DialogSize.Medium,
            Modal = true,
            PreventDismissOnEscape = true,
        };

        DialogResult result = await DialogService.ShowDialogAsync<CreateProductDialog>(options: options);

        if (!result.Cancelled)
        {
            await _grid.RefreshDataAsync();
        }
    }

    private async Task OpenPriceDialogAsync(ProductRowViewModel product)
    {
        DialogOptions options = new()
        {
            Header = { Title = $"Reprice {product.Sku}" },
            Size = DialogSize.Small,
            Modal = true,
            PreventDismissOnEscape = true,
            Parameters = new Dictionary<string, object?>(comparer: StringComparer.Ordinal)
            {
                [nameof(UpdateProductPriceDialog.ProductId)] = product.Id,
                [nameof(UpdateProductPriceDialog.Sku)] = product.Sku,
                [nameof(UpdateProductPriceDialog.CurrentAmount)] = product.PriceAmount,
                [nameof(UpdateProductPriceDialog.CurrentCurrency)] = product.PriceCurrency,
            },
        };

        DialogResult result = await DialogService.ShowDialogAsync<UpdateProductPriceDialog>(options: options);

        if (!result.Cancelled)
        {
            await _grid.RefreshDataAsync();
        }
    }

    private async Task OpenStockDialogAsync(ProductRowViewModel product)
    {
        DialogOptions options = new()
        {
            Header = { Title = $"Adjust stock — {product.Sku}" },
            Size = DialogSize.Small,
            Modal = true,
            PreventDismissOnEscape = true,
            Parameters = new Dictionary<string, object?>(comparer: StringComparer.Ordinal)
            {
                [nameof(AdjustStockDialog.ProductId)] = product.Id,
                [nameof(AdjustStockDialog.Sku)] = product.Sku,
                [nameof(AdjustStockDialog.CurrentStock)] = product.StockQuantity,
            },
        };

        DialogResult result = await DialogService.ShowDialogAsync<AdjustStockDialog>(options: options);

        if (!result.Cancelled)
        {
            await _grid.RefreshDataAsync();
        }
    }
}
