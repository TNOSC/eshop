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
using Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.Services;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Features.Admin.Identity;

public sealed class AdminCustomersServiceTests
{
    private readonly IIdentityApi _identityApi = Substitute.For<IIdentityApi>();
    private readonly AdminCustomersService _sut;

    public AdminCustomersServiceTests() => _sut = new AdminCustomersService(identityApi: _identityApi);

    [Fact]
    public async Task SearchAsync_Should_SearchUnfiltered_WithTheGivenPaging()
    {
        // Arrange
        _identityApi.SearchCustomersAsync(
                search: Arg.Any<string?>(),
                isActive: Arg.Any<bool?>(),
                page: Arg.Any<int>(),
                pageSize: Arg.Any<int>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<PagedResult<CustomerSummary>>.Success(
                value: new PagedResult<CustomerSummary>(Items: [], Page: 1, PageSize: 20, TotalCount: 0, TotalPages: 0))));

        // Act
        await _sut.SearchAsync(page: 1, pageSize: 20, cancellationToken: CancellationToken.None);

        // Assert
        await _identityApi.Received(requiredNumberOfCalls: 1).SearchCustomersAsync(
            search: null,
            isActive: null,
            page: 1,
            pageSize: 20,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_Should_MapEachCustomerSummaryIntoARowViewModel()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        var customer = new CustomerSummary(Id: customerId, Email: "a@b.com", FirstName: "Jane", LastName: "Doe", IsActive: true);

        _identityApi.SearchCustomersAsync(
                search: Arg.Any<string?>(),
                isActive: Arg.Any<bool?>(),
                page: Arg.Any<int>(),
                pageSize: Arg.Any<int>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<PagedResult<CustomerSummary>>.Success(
                value: new PagedResult<CustomerSummary>(Items: [customer], Page: 1, PageSize: 20, TotalCount: 1, TotalPages: 1))));

        // Act
        ClientResult<PagedResult<CustomerRowViewModel>> result = await _sut.SearchAsync(page: 1, pageSize: 20, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        CustomerRowViewModel mapped = result.Value.Items.ShouldHaveSingleItem();
        mapped.Id.ShouldBe(expected: customerId);
        mapped.Email.ShouldBe(expected: "a@b.com");
        mapped.FirstName.ShouldBe(expected: "Jane");
        mapped.LastName.ShouldBe(expected: "Doe");
        mapped.IsActive.ShouldBeTrue();
    }
}
