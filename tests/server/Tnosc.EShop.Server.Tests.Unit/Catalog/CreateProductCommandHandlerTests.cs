// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Application.Catalog.Commands.CreateProduct;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Catalog;

/// <summary>
/// The handler orchestrates and nothing more: build value objects, delegate to the domain factory,
/// persist, commit. Every failure it returns is the domain's verdict, passed through untouched.
/// </summary>
public sealed class CreateProductCommandHandlerTests
{
    private readonly Faker _faker = CatalogFaker.New();
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task HandleAsync_Should_AddTheProductAndCommit_When_TheCommandIsValid()
    {
        // Arrange
        string sku = _faker.Sku();
        NoProductHoldsTheSku();

        // Act
        Result<ProductId> result = await HandleAsync(command: ValidCommand() with { Sku = sku });

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _repository.Received(requiredNumberOfCalls: 1).AddAsync(
            aggregate: Arg.Is<Product>(predicate: product => product.Sku.Value == sku),
            cancellationToken: Arg.Any<CancellationToken>());
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnTheIdOfTheProductItAdded()
    {
        // Arrange
        NoProductHoldsTheSku();
        Product? added = null;
        await _repository.AddAsync(
            aggregate: Arg.Do<Product>(useArgument: product => added = product),
            cancellationToken: Arg.Any<CancellationToken>());

        // Act
        Result<ProductId> result = await HandleAsync(command: ValidCommand());

        // Assert
        result.Value.ShouldBe(expected: added!.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheConflict_Unchanged_When_TheSkuIsAlreadyTaken()
    {
        // Arrange
        string sku = _faker.Sku();
        Product existing = await ProductTestFactory.CreateAsync(sku: sku);
        _repository
            .GetBySkuAsync(sku: Arg.Any<Sku>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: existing));

        // Act
        Result<ProductId> result = await HandleAsync(command: ValidCommand() with { Sku = sku });

        // Assert
        // The handler must not reinterpret the domain's verdict — Conflict stays Conflict, and the
        // code stays the one the factory chose.
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Product.SkuAlreadyExists");
    }

    [Fact]
    public async Task HandleAsync_Should_NotCommit_When_TheDomainRejectsTheCommand()
    {
        // Arrange
        string sku = _faker.Sku();
        Product existing = await ProductTestFactory.CreateAsync(sku: sku);
        _repository
            .GetBySkuAsync(sku: Arg.Any<Sku>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: existing));

        // Act
        await HandleAsync(command: ValidCommand() with { Sku = sku });

        // Assert
        await _repository.DidNotReceive().AddAsync(aggregate: Arg.Any<Product>(), cancellationToken: Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheSkuFormatError_And_NeverReachTheRepository()
    {
        // Act
        Result<ProductId> result = await HandleAsync(command: ValidCommand() with { Sku = "not a sku" });

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "Sku.InvalidFormat");
        await _repository.DidNotReceive().GetBySkuAsync(sku: Arg.Any<Sku>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheNegativeAmountError()
    {
        // Act
        Result<ProductId> result = await HandleAsync(command: ValidCommand() with { PriceAmount = -_faker.PriceAmount() });

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Money.NegativeAmount");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheNegativeStockError()
    {
        // Arrange
        NoProductHoldsTheSku();

        // Act
        Result<ProductId> result = await HandleAsync(command: ValidCommand() with { StockQuantity = -_faker.Random.Int(min: 1, max: 100) });

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "StockQuantity.Negative");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    private void NoProductHoldsTheSku() =>
        _repository
            .GetBySkuAsync(sku: Arg.Any<Sku>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: null));

    private CreateProductCommand ValidCommand() =>
        new(Sku: _faker.Sku(),
            Name: _faker.ProductName(),
            Description: _faker.Description(),
            PriceAmount: _faker.PriceAmount(),
            PriceCurrency: _faker.Currency(),
            StockQuantity: _faker.StockQuantity(),
            BrandId: Guid.CreateVersion7(),
            CategoryId: Guid.CreateVersion7());

    private ValueTask<Result<ProductId>> HandleAsync(CreateProductCommand command) =>
        new CreateProductCommandHandler(repository: _repository, unitOfWork: _unitOfWork)
            .HandleAsync(command: command, cancellationToken: CancellationToken.None);
}
