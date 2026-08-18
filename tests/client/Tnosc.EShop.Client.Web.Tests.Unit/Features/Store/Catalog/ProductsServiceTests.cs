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
        ClientResult<PagedResult<ProductSummary>> result = await _sut.SearchAsync(
            viewModel: viewModel,
            pageSize: 12,
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem.ShouldBe(expected: problem);
    }
}
