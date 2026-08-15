// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Ordering;
using Tnosc.Lib.Web.Api;
using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders;

/// <summary>The caller's own order history: a server-paged grid, newest first.</summary>
public partial class MyOrders : ComponentBase
{
    private const int PageSize = 20;

    private readonly PaginationState _pagination = new() { ItemsPerPage = PageSize };

    private GridItemsProvider<OrderSummary> _ordersProvider = default!;
    private ApiProblem? _problem;

    [Inject]
    public IOrderingApi OrderingApi { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    protected override void OnInitialized() => _ordersProvider = ProvideOrdersAsync;

    private async ValueTask<GridItemsProviderResult<OrderSummary>> ProvideOrdersAsync(
        GridItemsProviderRequest<OrderSummary> request)
    {
        int page = (request.StartIndex / PageSize) + 1;

        ApiResult<PagedResult<OrderSummary>> result = await OrderingApi.GetMyOrdersAsync(
            page: page,
            pageSize: PageSize,
            cancellationToken: request.CancellationToken);

        if (!result.IsSuccess)
        {
            _problem = result.Problem;
            return GridItemsProviderResult.From<OrderSummary>(items: [], totalItemCount: 0);
        }

        _problem = null;
        return GridItemsProviderResult.From<OrderSummary>(
            items: [.. result.Value.Items],
            totalItemCount: (int)result.Value.TotalCount);
    }
}
