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
using Tnosc.EShop.Server.Application.Basket.Commands.ChangeBasketItemQuantity;
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.EShop.Server.Domain.Basket.Baskets;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Shared.Results;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Tests.Unit.Basket;

/// <summary>
/// Loads the caller's basket and delegates the transition to the aggregate.
/// </summary>
public sealed class ChangeBasketItemQuantityCommandHandlerTests
{
    private readonly Faker _faker = BasketFaker.New();
    private readonly IBasketRepository _repository = Substitute.For<IBasketRepository>();

    [Fact]
    public async Task HandleAsync_Should_ReplaceTheQuantityAndSave_When_TheLineExists()
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
        BasketExistsFor(customerId: customerId, basket: basket);

        // Act
        Result<BasketDto> result = await HandleAsync(command: new ChangeBasketItemQuantityCommand(CustomerId: customerId, ItemId: itemId, Quantity: 9));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Single().Quantity.ShouldBe(expected: 9);
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
        Result<BasketDto> result = await HandleAsync(command: new ChangeBasketItemQuantityCommand(CustomerId: customerId, ItemId: Guid.CreateVersion7(), Quantity: 1));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Basket.ItemNotFound");
        await _repository.DidNotReceive().SaveAsync(basket: Arg.Any<BasketAggregate>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheDomainsNotFound_And_NeverSave_When_TheLineIsMissing()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        var basket = BasketAggregate.CreateFor(customerId: customerId);
        BasketExistsFor(customerId: customerId, basket: basket);

        // Act
        Result<BasketDto> result = await HandleAsync(command: new ChangeBasketItemQuantityCommand(CustomerId: customerId, ItemId: Guid.CreateVersion7(), Quantity: 1));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Basket.ItemNotFound");
        await _repository.DidNotReceive().SaveAsync(basket: Arg.Any<BasketAggregate>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    private void BasketExistsFor(Guid customerId, BasketAggregate basket) =>
        _repository
            .GetByCustomerIdAsync(customerId: customerId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<BasketAggregate?>(result: basket));

    private ValueTask<Result<BasketDto>> HandleAsync(ChangeBasketItemQuantityCommand command) =>
        new ChangeBasketItemQuantityCommandHandler(repository: _repository)
            .HandleAsync(command: command, cancellationToken: CancellationToken.None);
}
