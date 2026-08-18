// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Components.Shared;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Pages;

/// <summary>The back-office landing page: tiles linking to the Catalog and Identity consoles.</summary>
public partial class AdminDashboardPage : ComponentBase
{
    private ComponentState _state = ComponentState.Loading;
    private long? _productCount;
    private long? _customerCount;
    private ClientProblem? _productsProblem;
    private ClientProblem? _customersProblem;

    [Inject]
    public ICatalogApi CatalogApi { get; set; } = null!;

    [Inject]
    public IIdentityApi IdentityApi { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        SearchProductsQuery query = new(Search: null, CategoryId: null, Page: 1, PageSize: 1);

        ClientResult<PagedResult<ProductSummary>> products = await CatalogApi.SearchProductsAsync(
            query: query,
            cancellationToken: CancellationToken.None);

        if (products.IsSuccess)
        {
            _productCount = products.Value.TotalCount;
        }
        else
        {
            _productsProblem = products.Problem;
        }

        ClientResult<PagedResult<CustomerSummary>> customers = await IdentityApi.SearchCustomersAsync(
            search: null,
            isActive: null,
            page: 1,
            pageSize: 1,
            cancellationToken: CancellationToken.None);

        if (customers.IsSuccess)
        {
            _customerCount = customers.Value.TotalCount;
        }
        else
        {
            _customersProblem = customers.Problem;
        }

        _state = ComponentState.Content;
    }
}
