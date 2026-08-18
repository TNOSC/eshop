// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Features.Admin.Catalog;

public sealed class AdminProductsServiceTests
{
    private readonly ICatalogApi _catalogApi = Substitute.For<ICatalogApi>();
    private readonly AdminProductsService _sut;

    public AdminProductsServiceTests() => _sut = new AdminProductsService(catalogApi: _catalogApi);

    [Fact]
    public async Task SearchAsync_Should_BuildAnUnfilteredQuery_WithTheGivenPaging()
    {
        // Arrange
        _catalogApi.SearchProductsAsync(query: Arg.Any<SearchProductsQuery>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<PagedResult<ProductSummary>>.Success(
                value: new PagedResult<ProductSummary>(Items: [], Page: 2, PageSize: 20, TotalCount: 0, TotalPages: 0))));

        // Act
        await _sut.SearchAsync(page: 2, pageSize: 20, cancellationToken: CancellationToken.None);

        // Assert
        await _catalogApi.Received(requiredNumberOfCalls: 1).SearchProductsAsync(
            query: Arg.Is<SearchProductsQuery>(predicate: q =>
                q.Search == null && q.CategoryId == null && q.Page == 2 && q.PageSize == 20),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_Should_MapEachProductSummaryIntoARowViewModel()
    {
        // Arrange
        var productId = Guid.CreateVersion7();
        var product = new ProductSummary(
            Id: productId,
            Sku: "SKU-1",
            Name: "Widget",
            PriceAmount: 9.99m,
            PriceCurrency: "USD",
            StockQuantity: 5,
            BrandName: "Brand",
            CategoryName: "Category");

        _catalogApi.SearchProductsAsync(query: Arg.Any<SearchProductsQuery>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<PagedResult<ProductSummary>>.Success(
                value: new PagedResult<ProductSummary>(Items: [product], Page: 1, PageSize: 20, TotalCount: 1, TotalPages: 1))));

        // Act
        ClientResult<PagedResult<ProductRowViewModel>> result = await _sut.SearchAsync(page: 1, pageSize: 20, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        ProductRowViewModel mapped = result.Value.Items.ShouldHaveSingleItem();
        mapped.Id.ShouldBe(expected: productId);
        mapped.Sku.ShouldBe(expected: "SKU-1");
        mapped.Name.ShouldBe(expected: "Widget");
        mapped.BrandName.ShouldBe(expected: "Brand");
        mapped.CategoryName.ShouldBe(expected: "Category");
        mapped.PriceAmount.ShouldBe(expected: 9.99m);
        mapped.PriceCurrency.ShouldBe(expected: "USD");
        mapped.StockQuantity.ShouldBe(expected: 5);
    }
}
