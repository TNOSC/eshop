// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Application.Basket.Commands.RemoveBasketItem;
using Tnosc.EShop.Server.Domain.Basket.Baskets;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain.Results;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Tests.Unit.Basket;

/// <summary>
/// Loads the caller's basket and delegates removal to the aggregate.
/// </summary>
public sealed class RemoveBasketItemCommandHandlerTests
{
    private readonly Faker _faker = BasketFaker.New();
    private readonly IBasketRepository _repository = Substitute.For<IBasketRepository>();

    [Fact]
    public async Task HandleAsync_Should_RemoveTheLineAndSave_When_ItExists()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        var basket = BasketAggregate.CreateFor(customerId: customerId);
        basket.AddItem(
            productId: _faker.ProductId(),
            sku: _faker.Sku(),
            productName: _faker.ProductName(),
            unitPrice: Money.Create(amount: _faker.PriceAmount(), currency: _faker.Currency()).Value,
            quantity: Quantity.Create(value: 1).Value);
        Guid itemId = basket.Items.Single().Id.Value;
        _repository
            .GetByCustomerIdAsync(customerId: customerId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<BasketAggregate?>(result: basket));

        // Act
        Result result = await HandleAsync(command: new RemoveBasketItemCommand(CustomerId: customerId, ItemId: itemId));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        basket.Items.ShouldBeEmpty();
        await _repository.Received(requiredNumberOfCalls: 1).SaveAsync(basket: basket, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_TheCustomerHasNoBasket()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        _repository
            .GetByCustomerIdAsync(customerId: customerId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<BasketAggregate?>(result: null));

        // Act
        Result result = await HandleAsync(command: new RemoveBasketItemCommand(CustomerId: customerId, ItemId: Guid.CreateVersion7()));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Basket.ItemNotFound");
        await _repository.DidNotReceive().SaveAsync(basket: Arg.Any<BasketAggregate>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    private ValueTask<Result> HandleAsync(RemoveBasketItemCommand command) =>
        new RemoveBasketItemCommandHandler(repository: _repository)
            .HandleAsync(command: command, cancellationToken: CancellationToken.None);
}
