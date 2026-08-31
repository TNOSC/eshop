// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Features.Store.Catalog;

public sealed class ProductsServiceTests
{
    private readonly ICatalogApi _catalogApi = Substitute.For<ICatalogApi>();
    private readonly ProductsService _sut;

    public ProductsServiceTests() => _sut = new ProductsService(catalogApi: _catalogApi);

    [Fact]
    public async Task SearchAsync_Should_MapTheViewModelsFilterAndPagingStateIntoTheQuery()
    {
        // Arrange
        var categoryId = Guid.CreateVersion7();
        ProductsViewModel viewModel = new()
        {
            Search = "keyboard",
            CategoryId = categoryId,
            Page = 3,
        };

        _catalogApi.SearchProductsAsync(query: Arg.Any<SearchProductsQuery>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<PagedResult<ProductSummary>>.Success(
                value: new PagedResult<ProductSummary>(Items: [], Page: 3, PageSize: 12, TotalCount: 0, TotalPages: 0))));

        // Act
        await _sut.SearchAsync(viewModel: viewModel, pageSize: 12, cancellationToken: CancellationToken.None);

        // Assert
        await _catalogApi.Received(requiredNumberOfCalls: 1).SearchProductsAsync(
            query: Arg.Is<SearchProductsQuery>(predicate: q =>
                q.Search == "keyboard" && q.CategoryId == categoryId && q.Page == 3 && q.PageSize == 12),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_Should_PassThroughTheFailure_When_TheApiCallFails()
    {
        // Arrange
        ProductsViewModel viewModel = new();
        var problem = ClientProblem.FromStatus(status: 500);

        _catalogApi.SearchProductsAsync(query: Arg.Any<SearchProductsQuery>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<PagedResult<ProductSummary>>.Failure(problem: problem)));

        // Act
        ClientResult<PagedResult<ProductSummaryViewModel>> result = await _sut.SearchAsync(
            viewModel: viewModel,
            pageSize: 12,
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem.ShouldBe(expected: problem);
    }

    [Fact]
    public async Task SearchAsync_Should_MapEachProductSummaryIntoAViewModel()
    {
        // Arrange
        ProductsViewModel viewModel = new();
        var productId = Guid.CreateVersion7();
        var product = new ProductSummary(
            Id: productId,
            Sku: "SKU-1",
            Name: "Widget",
            PriceAmount: 9.99m,
            PriceCurrency: "USD",
            StockQuantity: 5,
            BrandName: "Brand",
            CategoryName: "Category",
            ImageUrl: null);

        _catalogApi.SearchProductsAsync(query: Arg.Any<SearchProductsQuery>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<PagedResult<ProductSummary>>.Success(
                value: new PagedResult<ProductSummary>(Items: [product], Page: 1, PageSize: 12, TotalCount: 1, TotalPages: 1))));

        // Act
        ClientResult<PagedResult<ProductSummaryViewModel>> result = await _sut.SearchAsync(
            viewModel: viewModel,
            pageSize: 12,
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        ProductSummaryViewModel mapped = result.Value.Items.ShouldHaveSingleItem();
        mapped.Id.ShouldBe(expected: productId);
        mapped.Sku.ShouldBe(expected: "SKU-1");
        mapped.Name.ShouldBe(expected: "Widget");
        mapped.PriceAmount.ShouldBe(expected: 9.99m);
        mapped.PriceCurrency.ShouldBe(expected: "USD");
        mapped.StockQuantity.ShouldBe(expected: 5);
    }

    [Fact]
    public async Task GetCategoriesAsync_Should_MapEachCategoryIntoAViewModel()
    {
        // Arrange
        var categoryId = Guid.CreateVersion7();
        var category = new Category(Id: categoryId, Name: "Peripherals");

        _catalogApi.GetCategoriesAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<IReadOnlyList<Category>>.Success(value: [category])));

        // Act
        ClientResult<IReadOnlyList<CategoryViewModel>> result = await _sut.GetCategoriesAsync(
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        CategoryViewModel mapped = result.Value.ShouldHaveSingleItem();
        mapped.Id.ShouldBe(expected: categoryId);
        mapped.Name.ShouldBe(expected: "Peripherals");
    }
}
