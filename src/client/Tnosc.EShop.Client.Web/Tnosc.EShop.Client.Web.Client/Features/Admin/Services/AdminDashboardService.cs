// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Services;

/// <inheritdoc cref="IAdminDashboardService" />
internal sealed class AdminDashboardService(ICatalogApi catalogApi, IIdentityApi identityApi) : IAdminDashboardService
{
    public async Task<AdminDashboardCounts> LoadCountsAsync(CancellationToken cancellationToken)
    {
        SearchProductsQuery query = new(Search: null, CategoryId: null, Page: 1, PageSize: 1);

        ClientResult<PagedResult<ProductSummary>> products = await catalogApi.SearchProductsAsync(
            query: query,
            cancellationToken: cancellationToken);

        ClientResult<PagedResult<CustomerSummary>> customers = await identityApi.SearchCustomersAsync(
            search: null,
            isActive: null,
            page: 1,
            pageSize: 1,
            cancellationToken: cancellationToken);

        return new AdminDashboardCounts(
            ProductCount: products.IsSuccess ? products.Value.TotalCount : null,
            ProductsProblem: products.IsSuccess ? null : products.Problem,
            CustomerCount: customers.IsSuccess ? customers.Value.TotalCount : null,
            CustomersProblem: customers.IsSuccess ? null : customers.Problem);
    }
}
