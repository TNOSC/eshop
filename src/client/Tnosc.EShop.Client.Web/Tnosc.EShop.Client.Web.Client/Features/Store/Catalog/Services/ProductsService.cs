// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Services;

/// <inheritdoc cref="IProductsService" />
internal sealed class ProductsService(ICatalogApi catalogApi) : IProductsService
{
    public async Task<ClientResult<IReadOnlyList<CategoryViewModel>>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        ClientResult<IReadOnlyList<Category>> result = await catalogApi.GetCategoriesAsync(cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ClientResult<IReadOnlyList<CategoryViewModel>>.Failure(problem: result.Problem!);
        }

        IReadOnlyList<CategoryViewModel> categories = [.. result.Value.Select(ToViewModel)];
        return ClientResult<IReadOnlyList<CategoryViewModel>>.Success(value: categories);
    }

    public async Task<ClientResult<PagedResult<ProductSummaryViewModel>>> SearchAsync(
        ProductsViewModel viewModel,
        int pageSize,
        CancellationToken cancellationToken)
    {
        SearchProductsQuery query = new(
            Search: viewModel.Search,
            CategoryId: viewModel.CategoryId,
            Page: viewModel.Page,
            PageSize: pageSize);

        ClientResult<PagedResult<ProductSummary>> result = await catalogApi.SearchProductsAsync(
            query: query,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ClientResult<PagedResult<ProductSummaryViewModel>>.Failure(problem: result.Problem!);
        }

        PagedResult<ProductSummaryViewModel> page = new(
            Items: [.. result.Value.Items.Select(ToViewModel)],
            Page: result.Value.Page,
            PageSize: result.Value.PageSize,
            TotalCount: result.Value.TotalCount,
            TotalPages: result.Value.TotalPages);

        return ClientResult<PagedResult<ProductSummaryViewModel>>.Success(value: page);
    }

    private static CategoryViewModel ToViewModel(Category category) =>
        new() { Id = category.Id, Name = category.Name };

    private static ProductSummaryViewModel ToViewModel(ProductSummary product) =>
        new()
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            PriceAmount = product.PriceAmount,
            PriceCurrency = product.PriceCurrency,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
        };
}
