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
using Tnosc.EShop.Client.Web.Client.Features.Store.Orders.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Orders.ViewModels;
using Tnosc.Lib.Web.Components.Shared;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders.Pages;

/// <summary>The caller's own order history: a paged list, newest first, with the page reflected in
/// the URL so a shared link and the back button reproduce the same view. Fetching is
/// <see cref="IMyOrdersService"/>'s responsibility.</summary>
public partial class MyOrdersPage : ComponentBase
{
    private const int PageSize = 20;

    private readonly PaginationState _pagination = new() { ItemsPerPage = PageSize };

    private IReadOnlyList<OrderSummaryViewModel> _orders = [];
    private ClientProblem? _problem;
    private ComponentState _state = ComponentState.Loading;

    [Inject]
    public IMyOrdersService Service { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "page")]
    [Parameter]
    public int? Page { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        int requestedPageIndex = Math.Max(val1: 0, val2: (Page ?? 1) - 1);

        if (_pagination.CurrentPageIndex != requestedPageIndex)
        {
            await _pagination.SetCurrentPageIndexAsync(pageIndex: requestedPageIndex);
        }

        await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        _state = ComponentState.Loading;
        _problem = null;

        ClientResult<PagedResult<OrderSummaryViewModel>> result = await Service.GetMyOrdersAsync(
            page: _pagination.CurrentPageIndex + 1,
            pageSize: PageSize,
            cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            _orders = result.Value.Items;
            await _pagination.SetTotalItemCountAsync(
                totalItemCount: (int)result.Value.TotalCount,
                force: false);
        }
        else
        {
            _problem = result.Problem;
            _orders = [];
        }

        _state = ComponentState.Content;
    }

    private string PageUri(int pageIndex)
    {
        int page = pageIndex + 1;

        Dictionary<string, object?> queryParameters = new(comparer: StringComparer.Ordinal)
        {
            ["page"] = page == 1 ? null : page.ToString(provider: CultureInfo.InvariantCulture),
        };

        return Navigation.GetUriWithQueryParameters(parameters: queryParameters);
    }
}
