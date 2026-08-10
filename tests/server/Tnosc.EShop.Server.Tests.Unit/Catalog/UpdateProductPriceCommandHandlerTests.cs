// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Application.Catalog.Commands.UpdateProductPrice;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Catalog;

/// <summary>
/// Load, delegate the transition to the aggregate, commit. The "may a discontinued product be
/// repriced" question is answered by <see cref="Product"/>, and the handler only carries the answer out.
/// </summary>
public sealed class UpdateProductPriceCommandHandlerTests
{
    private readonly Faker _faker = CatalogFaker.New();
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task HandleAsync_Should_RepriceTheProductAndCommit_When_TheProductExists()
    {
        // Arrange
        string currency = _faker.Currency();
        decimal newAmount = _faker.PriceAmount();
        Product product = await ProductTestFactory.CreateAsync(amount: _faker.PriceAmount(), currency: currency);
        ProductIsInTheRepository(product: product);

        // Act
        Result result = await HandleAsync(command: new UpdateProductPriceCommand(
            ProductId: product.Id.Value,
            Amount: newAmount,
            Currency: currency));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.Price.Amount.ShouldBe(expected: newAmount);
        _repository.Received(requiredNumberOfCalls: 1).Update(aggregate: product);
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_NoProductCarriesThatId()
    {
        // Arrange
        _repository
            .GetByIdAsync(id: Arg.Any<ProductId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: null));

        // Act
        Result result = await HandleAsync(command: new UpdateProductPriceCommand(
            ProductId: Guid.CreateVersion7(),
            Amount: _faker.PriceAmount(),
            Currency: _faker.Currency()));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Product.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheConflict_Unchanged_When_TheProductIsDiscontinued()
    {
        // Arrange
        decimal amount = _faker.PriceAmount();
        string currency = _faker.Currency();
        Product product = await ProductTestFactory.CreateAsync(amount: amount, currency: currency);
        product.Discontinue();
        ProductIsInTheRepository(product: product);

        // Act
        Result result = await HandleAsync(command: new UpdateProductPriceCommand(
            ProductId: product.Id.Value,
            Amount: _faker.PriceAmount(),
            Currency: currency));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Product.Discontinued");
        product.Price.Amount.ShouldBe(expected: amount);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheCurrencyError_And_NeverLoadTheProduct()
    {
        // Act
        Result result = await HandleAsync(command: new UpdateProductPriceCommand(
            ProductId: Guid.CreateVersion7(),
            Amount: _faker.PriceAmount(),
            Currency: "euro"));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "Money.InvalidCurrency");
        await _repository.DidNotReceive().GetByIdAsync(id: Arg.Any<ProductId>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    private void ProductIsInTheRepository(Product product) =>
        _repository
            .GetByIdAsync(id: Arg.Any<ProductId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: product));

    private ValueTask<Result> HandleAsync(UpdateProductPriceCommand command) =>
        new UpdateProductPriceCommandHandler(repository: _repository, unitOfWork: _unitOfWork)
            .HandleAsync(command: command, cancellationToken: CancellationToken.None);
}
