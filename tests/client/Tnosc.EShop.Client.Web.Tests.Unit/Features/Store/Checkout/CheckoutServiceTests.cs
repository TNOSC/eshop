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
using Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Basket;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Features.Store.Checkout;

public sealed class CheckoutServiceTests
{
    private readonly IBasketApi _basketApi = Substitute.For<IBasketApi>();
    private readonly IIdentityApi _identityApi = Substitute.For<IIdentityApi>();
    private readonly IOrderingApi _orderingApi = Substitute.For<IOrderingApi>();
    private readonly CheckoutService _sut;

    public CheckoutServiceTests() => _sut = new CheckoutService(basketApi: _basketApi, identityApi: _identityApi, orderingApi: _orderingApi);

    [Fact]
    public async Task LoadAsync_Should_ReturnBothValues_When_BothCallsSucceed()
    {
        // Arrange
        var address = new CustomerAddress(Id: Guid.CreateVersion7(), Street: "St", City: "City", PostalCode: "0000", Country: "US");
        var item = new BasketItem(
            ItemId: Guid.CreateVersion7(),
            ProductId: Guid.CreateVersion7(),
            Sku: "SKU-1",
            ProductName: "Widget",
            UnitPriceAmount: 5m,
            UnitPriceCurrency: "USD",
            Quantity: 2);
        var basket = new BasketDto(BasketId: Guid.CreateVersion7(), CustomerId: Guid.CreateVersion7(), Items: [item], TotalAmount: 10m, TotalCurrency: "USD");
        var customer = new Customer(Id: Guid.CreateVersion7(), Email: "a@b.com", FirstName: "A", LastName: "B", PhoneNumber: null, IsActive: true, DefaultAddressId: address.Id, Addresses: [address]);

        _basketApi.GetBasketAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<BasketDto>.Success(value: basket)));
        _identityApi.GetMeAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<Customer>.Success(value: customer)));

        // Act
        CheckoutLoadResult result = await _sut.LoadAsync(cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Basket!.TotalAmount.ShouldBe(expected: basket.TotalAmount);
        result.Basket.TotalCurrency.ShouldBe(expected: basket.TotalCurrency);
        CheckoutBasketItemViewModel mappedItem = result.Basket.Items.ShouldHaveSingleItem();
        mappedItem.ItemId.ShouldBe(expected: item.ItemId);
        mappedItem.ProductName.ShouldBe(expected: item.ProductName);
        mappedItem.UnitPriceAmount.ShouldBe(expected: item.UnitPriceAmount);
        mappedItem.UnitPriceCurrency.ShouldBe(expected: item.UnitPriceCurrency);
        mappedItem.Quantity.ShouldBe(expected: item.Quantity);
        result.Customer!.DefaultAddressId.ShouldBe(expected: customer.DefaultAddressId);
        CheckoutAddressViewModel mappedAddress = result.Customer.Addresses.ShouldHaveSingleItem();
        mappedAddress.Id.ShouldBe(expected: address.Id);
        mappedAddress.Street.ShouldBe(expected: address.Street);
        mappedAddress.City.ShouldBe(expected: address.City);
        mappedAddress.PostalCode.ShouldBe(expected: address.PostalCode);
        mappedAddress.Country.ShouldBe(expected: address.Country);
    }

    [Fact]
    public async Task LoadAsync_Should_ReturnTheFailure_When_EitherCallFails()
    {
        // Arrange
        var problem = ClientProblem.FromStatus(status: 500);
        var customer = new Customer(Id: Guid.CreateVersion7(), Email: "a@b.com", FirstName: "A", LastName: "B", PhoneNumber: null, IsActive: true, DefaultAddressId: null, Addresses: []);

        _basketApi.GetBasketAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<BasketDto>.Failure(problem: problem)));
        _identityApi.GetMeAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<Customer>.Success(value: customer)));

        // Act
        CheckoutLoadResult result = await _sut.LoadAsync(cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem.ShouldBe(expected: problem);
    }

    [Fact]
    public async Task PlaceOrderAsync_Should_CallTheApi_WithTheIdempotencyKey()
    {
        // Arrange
        var idempotencyKey = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();

        _orderingApi.PlaceOrderAsync(idempotencyKey: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<Guid>.Success(value: orderId)));

        // Act
        ClientResult<Guid> result = await _sut.PlaceOrderAsync(idempotencyKey: idempotencyKey, cancellationToken: CancellationToken.None);

        // Assert
        result.Value.ShouldBe(expected: orderId);
        await _orderingApi.Received(requiredNumberOfCalls: 1).PlaceOrderAsync(
            idempotencyKey: idempotencyKey,
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
