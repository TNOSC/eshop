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
using Tnosc.EShop.Client.Web.Client.Features.Store.Basket.Services;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Basket;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Features.Store.Basket;

public sealed class BasketPageServiceTests
{
    private readonly IBasketApi _basketApi = Substitute.For<IBasketApi>();
    private readonly BasketPageService _sut;

    public BasketPageServiceTests() => _sut = new BasketPageService(basketApi: _basketApi);

    [Fact]
    public async Task GetBasketAsync_Should_CallTheApi()
    {
        // Arrange
        var basket = new BasketDto(BasketId: Guid.CreateVersion7(), CustomerId: Guid.CreateVersion7(), Items: [], TotalAmount: null, TotalCurrency: null);
        _basketApi.GetBasketAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<BasketDto>.Success(value: basket)));

        // Act
        ClientResult<BasketDto> result = await _sut.GetBasketAsync(cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _basketApi.Received(requiredNumberOfCalls: 1).GetBasketAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeQuantityAsync_Should_MapTheQuantityIntoTheRequest()
    {
        // Arrange
        var itemId = Guid.CreateVersion7();
        var basket = new BasketDto(BasketId: Guid.CreateVersion7(), CustomerId: Guid.CreateVersion7(), Items: [], TotalAmount: null, TotalCurrency: null);
        _basketApi.ChangeItemQuantityAsync(
                itemId: Arg.Any<Guid>(),
                request: Arg.Any<ChangeBasketItemQuantityRequest>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<BasketDto>.Success(value: basket)));

        // Act
        await _sut.ChangeQuantityAsync(itemId: itemId, quantity: 3, cancellationToken: CancellationToken.None);

        // Assert
        await _basketApi.Received(requiredNumberOfCalls: 1).ChangeItemQuantityAsync(
            itemId: itemId,
            request: Arg.Is<ChangeBasketItemQuantityRequest>(predicate: r => r.Quantity == 3),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveItemAsync_Should_CallTheApi()
    {
        // Arrange
        var itemId = Guid.CreateVersion7();
        _basketApi.RemoveItemAsync(itemId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult.Success()));

        // Act
        await _sut.RemoveItemAsync(itemId: itemId, cancellationToken: CancellationToken.None);

        // Assert
        await _basketApi.Received(requiredNumberOfCalls: 1).RemoveItemAsync(
            itemId: itemId,
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
