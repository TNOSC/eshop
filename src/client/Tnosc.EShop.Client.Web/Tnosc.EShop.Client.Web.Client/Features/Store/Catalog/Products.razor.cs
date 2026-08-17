// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Api;
using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog;

/// <summary>
/// The storefront's product gallery: search, category filter and paging, all reflected in the URL
/// so a shared link and the back button reproduce the same view — the same reason prerender is
/// deterministic here.
/// </summary>
public partial class Products : ComponentBase
{
    private const int PageSize = 12;

    private readonly PaginationState _pagination = new() { ItemsPerPage = PageSize };

    private IReadOnlyList<ProductSummary> _products = [];
    private IReadOnlyList<Category> _categories = [];
    private ApiProblem? _problem;
    private bool _isLoading = true;

    [Inject]
    public ICatalogApi CatalogApi { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "search")]
    [Parameter]
    public string? Search { get; set; }

    [SupplyParameterFromQuery(Name = "categoryId")]
    [Parameter]
    public Guid? CategoryId { get; set; }

    [SupplyParameterFromQuery(Name = "page")]
    [Parameter]
    public int? Page { get; set; }

    protected override async Task OnInitializedAsync()
    {
        ApiResult<IReadOnlyList<Category>> categories = await CatalogApi.GetCategoriesAsync(
            cancellationToken: CancellationToken.None);

        if (categories.IsSuccess)
        {
            _categories = categories.Value;
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        int requestedPageIndex = Math.Max(val1: 0, val2: (Page ?? 1) - 1);

        if (_pagination.CurrentPageIndex != requestedPageIndex)
        {
            await _pagination.SetCurrentPageIndexAsync(pageIndex: requestedPageIndex);
        }

        await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        _isLoading = true;
        _problem = null;

        SearchProductsQuery query = new(
            Search: Search,
            CategoryId: CategoryId,
            Page: _pagination.CurrentPageIndex + 1,
            PageSize: PageSize);

        ApiResult<PagedResult<ProductSummary>> result = await CatalogApi.SearchProductsAsync(
            query: query,
            cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            _products = result.Value.Items;
            await _pagination.SetTotalItemCountAsync(
                totalItemCount: (int)result.Value.TotalCount,
                force: false);
        }
        else
        {
            _problem = result.Problem;
            _products = [];
        }

        _isLoading = false;
    }

    private Task OnSearchChangedAsync(string? search) =>
        NavigateAsync(search: search, categoryId: CategoryId, page: 1);

    private string PageUri(int pageIndex)
    {
        int page = pageIndex + 1;

        Dictionary<string, object?> queryParameters = new(comparer: StringComparer.Ordinal)
        {
            ["search"] = string.IsNullOrWhiteSpace(value: Search) ? null : Search,
            ["categoryId"] = CategoryId,
            ["page"] = page == 1 ? null : page.ToString(provider: CultureInfo.InvariantCulture),
        };

        return Navigation.GetUriWithQueryParameters(parameters: queryParameters);
    }

    private Task NavigateAsync(string? search, Guid? categoryId, int page)
    {
        Dictionary<string, object?> queryParameters = new(comparer: StringComparer.Ordinal)
        {
            ["search"] = string.IsNullOrWhiteSpace(value: search) ? null : search,
            ["categoryId"] = categoryId,
            ["page"] = page == 1 ? null : page.ToString(provider: CultureInfo.InvariantCulture),
        };

        string uri = Navigation.GetUriWithQueryParameters(parameters: queryParameters);
        Navigation.NavigateTo(uri: uri);
        return Task.CompletedTask;
    }
}
