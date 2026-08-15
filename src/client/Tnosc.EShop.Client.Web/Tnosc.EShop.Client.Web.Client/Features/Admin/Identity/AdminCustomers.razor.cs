// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Api;
using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Identity;

/// <summary>The admin customer console: a server-paged grid, one row per customer.</summary>
public partial class AdminCustomers : ComponentBase
{
    private const int PageSize = 20;

    private readonly PaginationState _pagination = new() { ItemsPerPage = PageSize };

    private GridItemsProvider<CustomerSummary> _customersProvider = default!;
    private ApiProblem? _problem;

    [Inject]
    public IIdentityApi IdentityApi { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    protected override void OnInitialized() => _customersProvider = ProvideCustomersAsync;

    private async ValueTask<GridItemsProviderResult<CustomerSummary>> ProvideCustomersAsync(
        GridItemsProviderRequest<CustomerSummary> request)
    {
        int page = (request.StartIndex / PageSize) + 1;

        ApiResult<PagedResult<CustomerSummary>> result = await IdentityApi.SearchCustomersAsync(
            search: null,
            isActive: null,
            page: page,
            pageSize: PageSize,
            cancellationToken: request.CancellationToken);

        if (!result.IsSuccess)
        {
            _problem = result.Problem;
            return GridItemsProviderResult.From<CustomerSummary>(items: [], totalItemCount: 0);
        }

        _problem = null;
        return GridItemsProviderResult.From<CustomerSummary>(
            items: [.. result.Value.Items],
            totalItemCount: (int)result.Value.TotalCount);
    }
}
