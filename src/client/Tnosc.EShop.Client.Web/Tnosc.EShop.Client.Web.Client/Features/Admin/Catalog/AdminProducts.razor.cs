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
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Api;
using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog;

/// <summary>The admin product console: a server-paged grid plus create/price/stock dialogs.</summary>
public partial class AdminProducts : ComponentBase
{
    private const int PageSize = 20;

    private readonly PaginationState _pagination = new() { ItemsPerPage = PageSize };

    private FluentDataGrid<ProductSummary> _grid = default!;
    private GridItemsProvider<ProductSummary> _productsProvider = default!;
    private ApiProblem? _problem;

    [Inject]
    public ICatalogApi CatalogApi { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override void OnInitialized() => _productsProvider = ProvideProductsAsync;

    private async ValueTask<GridItemsProviderResult<ProductSummary>> ProvideProductsAsync(
        GridItemsProviderRequest<ProductSummary> request)
    {
        int page = (request.StartIndex / PageSize) + 1;

        SearchProductsQuery query = new(Search: null, CategoryId: null, Page: page, PageSize: PageSize);

        ApiResult<PagedResult<ProductSummary>> result = await CatalogApi.SearchProductsAsync(
            query: query,
            cancellationToken: request.CancellationToken);

        if (!result.IsSuccess)
        {
            _problem = result.Problem;
            return GridItemsProviderResult.From<ProductSummary>(items: [], totalItemCount: 0);
        }

        _problem = null;
        return GridItemsProviderResult.From<ProductSummary>(
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

    private async Task OpenPriceDialogAsync(ProductSummary product)
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

    private async Task OpenStockDialogAsync(ProductSummary product)
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
