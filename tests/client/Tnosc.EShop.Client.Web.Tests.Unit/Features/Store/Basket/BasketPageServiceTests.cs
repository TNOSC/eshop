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
using Tnosc.EShop.Client.Web.Client.Features.Store.Basket.ViewModels;
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
    public async Task GetBasketAsync_Should_MapTheBasketIntoAViewModel()
    {
        // Arrange
        var item = new BasketItem(
            ItemId: Guid.CreateVersion7(),
            ProductId: Guid.CreateVersion7(),
            Sku: "SKU-1",
            ProductName: "Widget",
            UnitPriceAmount: 5m,
            UnitPriceCurrency: "USD",
            Quantity: 2);
        var basket = new BasketDto(BasketId: Guid.CreateVersion7(), CustomerId: Guid.CreateVersion7(), Items: [item], TotalAmount: 10m, TotalCurrency: "USD");
        _basketApi.GetBasketAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<BasketDto>.Success(value: basket)));

        // Act
        ClientResult<BasketViewModel> result = await _sut.GetBasketAsync(cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalAmount.ShouldBe(expected: basket.TotalAmount);
        result.Value.TotalCurrency.ShouldBe(expected: basket.TotalCurrency);
        BasketItemViewModel mappedItem = result.Value.Items.ShouldHaveSingleItem();
        mappedItem.ItemId.ShouldBe(expected: item.ItemId);
        mappedItem.Sku.ShouldBe(expected: item.Sku);
        mappedItem.ProductName.ShouldBe(expected: item.ProductName);
        mappedItem.UnitPriceAmount.ShouldBe(expected: item.UnitPriceAmount);
        mappedItem.UnitPriceCurrency.ShouldBe(expected: item.UnitPriceCurrency);
        mappedItem.Quantity.ShouldBe(expected: item.Quantity);
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
