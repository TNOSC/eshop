// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.Services;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.ViewModels;
using Tnosc.Lib.Web.Components.Shared;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.Pages;

/// <summary>The admin customer console: a server-paged grid, one row per customer. Fetching is
/// <see cref="IAdminCustomersService"/>'s responsibility.</summary>
public partial class AdminCustomersPage : ComponentBase
{
    private const int PageSize = 20;

    private readonly PaginationState _pagination = new() { ItemsPerPage = PageSize };

    // The grid mounts and shows its own built-in loading spinner immediately, so there is no
    // separate "not yet mounted" state to gate on here — ComponentState only ever reaches Content
    // (or Error, if rendering the grid itself throws).
    private readonly ComponentState _state = ComponentState.Content;
    private GridItemsProvider<CustomerRowViewModel> _customersProvider = default!;
    private ClientProblem? _problem;

    [Inject]
    public IAdminCustomersService Service { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    protected override void OnInitialized() => _customersProvider = ProvideCustomersAsync;

    private async ValueTask<GridItemsProviderResult<CustomerRowViewModel>> ProvideCustomersAsync(
        GridItemsProviderRequest<CustomerRowViewModel> request)
    {
        int page = (request.StartIndex / PageSize) + 1;

        ClientResult<PagedResult<CustomerRowViewModel>> result = await Service.SearchAsync(
            page: page,
            pageSize: PageSize,
            cancellationToken: request.CancellationToken);

        if (!result.IsSuccess)
        {
            _problem = result.Problem;
            return GridItemsProviderResult.From<CustomerRowViewModel>(items: [], totalItemCount: 0);
        }

        _problem = null;
        return GridItemsProviderResult.From<CustomerRowViewModel>(
            items: [.. result.Value.Items],
            totalItemCount: (int)result.Value.TotalCount);
    }
}
