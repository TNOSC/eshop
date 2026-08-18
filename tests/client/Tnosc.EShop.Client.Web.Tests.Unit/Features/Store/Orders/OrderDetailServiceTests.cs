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
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Ordering;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Features.Store.Orders;

public sealed class OrderDetailServiceTests
{
    private readonly IOrderingApi _orderingApi = Substitute.For<IOrderingApi>();
    private readonly OrderDetailService _sut;

    public OrderDetailServiceTests() => _sut = new OrderDetailService(orderingApi: _orderingApi);

    [Fact]
    public async Task GetOrderByIdAsync_Should_CallTheApi_WithTheGivenId()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var order = new Order(
            Id: id,
            OrderNumber: "ORD-1",
            CustomerId: Guid.CreateVersion7(),
            Status: "Pending",
            TotalAmount: 10m,
            TotalCurrency: "USD",
            PlacedOnUtc: DateTime.UtcNow,
            ShippingStreet: "St",
            ShippingCity: "City",
            ShippingPostalCode: "0000",
            ShippingCountry: "US",
            Lines: []);

        _orderingApi.GetOrderByIdAsync(id: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<Order>.Success(value: order)));

        // Act
        ClientResult<Order> result = await _sut.GetOrderByIdAsync(id: id, cancellationToken: CancellationToken.None);

        // Assert
        result.Value.ShouldBe(expected: order);
        await _orderingApi.Received(requiredNumberOfCalls: 1).GetOrderByIdAsync(id: id, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmOrderAsync_Should_CallTheApi_WithTheGivenId()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        _orderingApi.ConfirmOrderAsync(id: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult.Success()));

        // Act
        await _sut.ConfirmOrderAsync(id: id, cancellationToken: CancellationToken.None);

        // Assert
        await _orderingApi.Received(requiredNumberOfCalls: 1).ConfirmOrderAsync(id: id, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelOrderAsync_Should_CallTheApi_WithTheGivenId()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        _orderingApi.CancelOrderAsync(id: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult.Success()));

        // Act
        await _sut.CancelOrderAsync(id: id, cancellationToken: CancellationToken.None);

        // Assert
        await _orderingApi.Received(requiredNumberOfCalls: 1).CancelOrderAsync(id: id, cancellationToken: Arg.Any<CancellationToken>());
    }
}
