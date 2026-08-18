// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;

/// <inheritdoc cref="IAdminProductsService" />
internal sealed class AdminProductsService(ICatalogApi catalogApi) : IAdminProductsService
{
    public Task<ClientResult<PagedResult<ProductSummary>>> SearchAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        SearchProductsQuery query = new(Search: null, CategoryId: null, Page: page, PageSize: pageSize);
        return catalogApi.SearchProductsAsync(query: query, cancellationToken: cancellationToken);
    }
}
