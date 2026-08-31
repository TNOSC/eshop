// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;

/// <inheritdoc cref="IAdminProductsService" />
internal sealed class AdminProductsService(ICatalogApi catalogApi) : IAdminProductsService
{
    public async Task<ClientResult<PagedResult<ProductRowViewModel>>> SearchAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        SearchProductsQuery query = new(Search: null, CategoryId: null, Page: page, PageSize: pageSize);

        ClientResult<PagedResult<ProductSummary>> result = await catalogApi.SearchProductsAsync(
            query: query,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ClientResult<PagedResult<ProductRowViewModel>>.Failure(problem: result.Problem!);
        }

        PagedResult<ProductRowViewModel> mappedPage = new(
            Items: [.. result.Value.Items.Select(ToViewModel)],
            Page: result.Value.Page,
            PageSize: result.Value.PageSize,
            TotalCount: result.Value.TotalCount,
            TotalPages: result.Value.TotalPages);

        return ClientResult<PagedResult<ProductRowViewModel>>.Success(value: mappedPage);
    }

    private static ProductRowViewModel ToViewModel(ProductSummary product) =>
        new()
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            BrandName = product.BrandName,
            CategoryName = product.CategoryName,
            PriceAmount = product.PriceAmount,
            PriceCurrency = product.PriceCurrency,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
        };
}
