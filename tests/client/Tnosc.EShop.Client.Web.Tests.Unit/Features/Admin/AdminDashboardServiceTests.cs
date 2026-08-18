// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Services;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Features.Admin;

public sealed class AdminDashboardServiceTests
{
    private readonly ICatalogApi _catalogApi = Substitute.For<ICatalogApi>();
    private readonly IIdentityApi _identityApi = Substitute.For<IIdentityApi>();
    private readonly AdminDashboardService _sut;

    public AdminDashboardServiceTests() => _sut = new AdminDashboardService(catalogApi: _catalogApi, identityApi: _identityApi);

    [Fact]
    public async Task LoadCountsAsync_Should_ReturnBothCounts_When_BothCallsSucceed()
    {
        // Arrange
        _catalogApi.SearchProductsAsync(query: Arg.Any<SearchProductsQuery>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<PagedResult<ProductSummary>>.Success(
                value: new PagedResult<ProductSummary>(Items: [], Page: 1, PageSize: 1, TotalCount: 42, TotalPages: 42))));

        _identityApi.SearchCustomersAsync(
                search: Arg.Any<string?>(),
                isActive: Arg.Any<bool?>(),
                page: Arg.Any<int>(),
                pageSize: Arg.Any<int>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<PagedResult<CustomerSummary>>.Success(
                value: new PagedResult<CustomerSummary>(Items: [], Page: 1, PageSize: 1, TotalCount: 7, TotalPages: 7))));

        // Act
        AdminDashboardCounts counts = await _sut.LoadCountsAsync(cancellationToken: CancellationToken.None);

        // Assert
        counts.ProductCount.ShouldBe(expected: 42);
        counts.ProductsProblem.ShouldBeNull();
        counts.CustomerCount.ShouldBe(expected: 7);
        counts.CustomersProblem.ShouldBeNull();
    }

    [Fact]
    public async Task LoadCountsAsync_Should_ReturnTheProblem_When_TheCatalogCallFails()
    {
        // Arrange
        var problem = ClientProblem.FromStatus(status: 500);

        _catalogApi.SearchProductsAsync(query: Arg.Any<SearchProductsQuery>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<PagedResult<ProductSummary>>.Failure(problem: problem)));

        _identityApi.SearchCustomersAsync(
                search: Arg.Any<string?>(),
                isActive: Arg.Any<bool?>(),
                page: Arg.Any<int>(),
                pageSize: Arg.Any<int>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<PagedResult<CustomerSummary>>.Success(
                value: new PagedResult<CustomerSummary>(Items: [], Page: 1, PageSize: 1, TotalCount: 0, TotalPages: 0))));

        // Act
        AdminDashboardCounts counts = await _sut.LoadCountsAsync(cancellationToken: CancellationToken.None);

        // Assert
        counts.ProductCount.ShouldBeNull();
        counts.ProductsProblem.ShouldBe(expected: problem);
    }
}
