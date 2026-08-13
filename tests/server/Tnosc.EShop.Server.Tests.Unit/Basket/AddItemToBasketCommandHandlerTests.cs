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
using Tnosc.EShop.Server.Application.Basket.Commands.AddItemToBasket;
using Tnosc.EShop.Server.Application.Basket.Ports;
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.EShop.Server.Domain.Basket.Baskets;
using Tnosc.Lib.Domain.Results;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Tests.Unit.Basket;

/// <summary>
/// Looks up the product's snapshot, gets-or-creates the basket, delegates to the aggregate, and
/// persists. Not-found and out-of-range paths must propagate the domain's verdict unchanged.
/// </summary>
public sealed class AddItemToBasketCommandHandlerTests
{
    private readonly Faker _faker = BasketFaker.New();
    private readonly IBasketRepository _repository = Substitute.For<IBasketRepository>();
    private readonly IProductLookup _productLookup = Substitute.For<IProductLookup>();

    [Fact]
    public async Task HandleAsync_Should_CreateTheBasketAndAddTheLine_When_TheCustomerHasNoBasketYet()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        ProductSnapshot product = SomeProduct();
        NoBasketExistsFor(customerId: customerId);
        ProductLookupReturns(product: product);

        // Act
        Result<BasketDto> result = await HandleAsync(command: new AddItemToBasketCommand(CustomerId: customerId, ProductId: product.ProductId, Quantity: 2));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CustomerId.ShouldBe(expected: customerId);
        result.Value.Items.ShouldHaveSingleItem();
        result.Value.Items.Single().ProductId.ShouldBe(expected: product.ProductId);
        result.Value.Items.Single().Quantity.ShouldBe(expected: 2);
        await _repository.Received(requiredNumberOfCalls: 1).SaveAsync(basket: Arg.Any<BasketAggregate>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_And_NeverSave_When_TheProductDoesNotExist()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        Guid productId = _faker.ProductId();
        NoBasketExistsFor(customerId: customerId);
        _productLookup
            .GetAsync(productId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<ProductSnapshot?>(result: null));

        // Act
        Result<BasketDto> result = await HandleAsync(command: new AddItemToBasketCommand(CustomerId: customerId, ProductId: productId, Quantity: 1));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Basket.ProductNotFound");
        await _repository.DidNotReceive().SaveAsync(basket: Arg.Any<BasketAggregate>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheQuantityError_And_NeverLookUpTheProduct_When_TheQuantityIsOutOfRange()
    {
        // Act
        Result<BasketDto> result = await HandleAsync(command: new AddItemToBasketCommand(CustomerId: _faker.CustomerId(), ProductId: _faker.ProductId(), Quantity: 0));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Quantity.OutOfRange");
        await _productLookup.DidNotReceive().GetAsync(productId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    private ProductSnapshot SomeProduct() =>
        new(ProductId: _faker.ProductId(), Sku: _faker.Sku(), Name: _faker.ProductName(), PriceAmount: _faker.PriceAmount(), PriceCurrency: _faker.Currency());

    private void NoBasketExistsFor(Guid customerId) =>
        _repository
            .GetByCustomerIdAsync(customerId: customerId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<BasketAggregate?>(result: null));

    private void ProductLookupReturns(ProductSnapshot product) =>
        _productLookup
            .GetAsync(productId: product.ProductId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<ProductSnapshot?>(result: product));

    private ValueTask<Result<BasketDto>> HandleAsync(AddItemToBasketCommand command) =>
        new AddItemToBasketCommandHandler(repository: _repository, productLookup: _productLookup)
            .HandleAsync(command: command, cancellationToken: CancellationToken.None);
}
