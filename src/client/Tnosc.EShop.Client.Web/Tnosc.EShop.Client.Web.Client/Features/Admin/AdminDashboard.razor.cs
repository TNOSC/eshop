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
using Tnosc.Lib.Web.Api;
using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin;

/// <summary>The back-office landing page: tiles linking to the Catalog and Identity consoles.</summary>
public partial class AdminDashboard : ComponentBase
{
    private long? _productCount;
    private long? _customerCount;

    [Inject]
    public ICatalogApi CatalogApi { get; set; } = null!;

    [Inject]
    public IIdentityApi IdentityApi { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        SearchProductsQuery query = new(Search: null, CategoryId: null, Page: 1, PageSize: 1);

        ApiResult<PagedResult<ProductSummary>> products = await CatalogApi.SearchProductsAsync(
            query: query,
            cancellationToken: CancellationToken.None);

        if (products.IsSuccess)
        {
            _productCount = products.Value.TotalCount;
        }

        ApiResult<PagedResult<CustomerSummary>> customers = await IdentityApi.SearchCustomersAsync(
            search: null,
            isActive: null,
            page: 1,
            pageSize: 1,
            cancellationToken: CancellationToken.None);

        if (customers.IsSuccess)
        {
            _customerCount = customers.Value.TotalCount;
        }
    }
}
