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
using Tnosc.EShop.Client.Web.Client.Features.Store.Orders.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Orders.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Ordering;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Features.Store.Orders;

public sealed class MyOrdersServiceTests
{
    private readonly IOrderingApi _orderingApi = Substitute.For<IOrderingApi>();
    private readonly MyOrdersService _sut;

    public MyOrdersServiceTests() => _sut = new MyOrdersService(orderingApi: _orderingApi);

    [Fact]
    public async Task GetMyOrdersAsync_Should_PassThroughThePagingParameters()
    {
        // Arrange
        _orderingApi.GetMyOrdersAsync(page: Arg.Any<int>(), pageSize: Arg.Any<int>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<PagedResult<OrderSummary>>.Success(
                value: new PagedResult<OrderSummary>(Items: [], Page: 2, PageSize: 20, TotalCount: 0, TotalPages: 0))));

        // Act
        await _sut.GetMyOrdersAsync(page: 2, pageSize: 20, cancellationToken: CancellationToken.None);

        // Assert
        await _orderingApi.Received(requiredNumberOfCalls: 1).GetMyOrdersAsync(
            page: 2,
            pageSize: 20,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMyOrdersAsync_Should_MapEachOrderSummaryIntoAViewModel()
    {
        // Arrange
        var orderId = Guid.CreateVersion7();
        DateTime placedOnUtc = DateTime.UtcNow;
        var order = new OrderSummary(
            Id: orderId,
            OrderNumber: "ORD-1",
            Status: "Pending",
            TotalAmount: 10m,
            TotalCurrency: "USD",
            PlacedOnUtc: placedOnUtc,
            LineCount: 2);

        _orderingApi.GetMyOrdersAsync(page: Arg.Any<int>(), pageSize: Arg.Any<int>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<PagedResult<OrderSummary>>.Success(
                value: new PagedResult<OrderSummary>(Items: [order], Page: 1, PageSize: 20, TotalCount: 1, TotalPages: 1))));

        // Act
        ClientResult<PagedResult<OrderSummaryViewModel>> result = await _sut.GetMyOrdersAsync(
            page: 1,
            pageSize: 20,
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        OrderSummaryViewModel mapped = result.Value.Items.ShouldHaveSingleItem();
        mapped.Id.ShouldBe(expected: orderId);
        mapped.OrderNumber.ShouldBe(expected: "ORD-1");
        mapped.Status.ShouldBe(expected: "Pending");
        mapped.TotalAmount.ShouldBe(expected: 10m);
        mapped.TotalCurrency.ShouldBe(expected: "USD");
        mapped.PlacedOnUtc.ShouldBe(expected: placedOnUtc);
    }
}
