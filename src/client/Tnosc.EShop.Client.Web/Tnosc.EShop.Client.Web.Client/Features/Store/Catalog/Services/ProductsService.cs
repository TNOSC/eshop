// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Services;

/// <inheritdoc cref="IProductsService" />
internal sealed class ProductsService(ICatalogApi catalogApi) : IProductsService
{
    public Task<ClientResult<IReadOnlyList<Category>>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        catalogApi.GetCategoriesAsync(cancellationToken: cancellationToken);

    public Task<ClientResult<PagedResult<ProductSummary>>> SearchAsync(
        ProductsViewModel viewModel,
        int pageSize,
        CancellationToken cancellationToken)
    {
        SearchProductsQuery query = new(
            Search: viewModel.Search,
            CategoryId: viewModel.CategoryId,
            Page: viewModel.Page,
            PageSize: pageSize);

        return catalogApi.SearchProductsAsync(query: query, cancellationToken: cancellationToken);
    }
}
