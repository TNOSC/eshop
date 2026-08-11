// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Application.Catalog.Commands.AdjustStock;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Catalog;

/// <summary>
/// Load, delegate the transition to the aggregate, commit — plus the opt-in retry that makes a lost
/// concurrency-token race worth re-running.
/// </summary>
public sealed class AdjustStockCommandHandlerTests
{
    private readonly Faker _faker = CatalogFaker.New();
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task HandleAsync_Should_MoveTheStockLevelAndCommit_When_TheProductExists()
    {
        // Arrange
        int stock = _faker.Random.Int(min: 5, max: 500);
        int delta = _faker.Random.Int(min: 1, max: 100);
        Product product = await ProductTestFactory.CreateAsync(stock: stock);
        ProductIsInTheRepository(product: product);

        // Act
        Result result = await HandleAsync(command: new AdjustStockCommand(ProductId: product.Id.Value, Delta: delta));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.Stock.Value.ShouldBe(expected: stock + delta);
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
        Result result = await HandleAsync(command: new AdjustStockCommand(ProductId: Guid.CreateVersion7(), Delta: _faker.Random.Int(min: 1, max: 100)));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Product.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheNegativeStockError_Unchanged_And_NotCommit()
    {
        // Arrange
        int stock = _faker.Random.Int(min: 1, max: 500);
        Product product = await ProductTestFactory.CreateAsync(stock: stock);
        ProductIsInTheRepository(product: product);

        // Act
        Result result = await HandleAsync(command: new AdjustStockCommand(ProductId: product.Id.Value, Delta: -(stock + 1)));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "StockQuantity.Negative");
        product.Stock.Value.ShouldBe(expected: stock);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Handler_Should_OptIntoRetry_But_NotIntoTransaction()
    {
        // Arrange
        Type handlerType = typeof(AdjustStockCommandHandler);

        // Act & Assert
        handlerType.GetCustomAttribute<RetryAttribute>().ShouldNotBeNull().MaxRetries.ShouldBe(expected: 3);
        handlerType.GetCustomAttributes<CacheTagAttribute>().ShouldHaveSingleItem().Tag.ShouldBe(expected: "catalog");

        // Single aggregate, single commit — TransactionDecorator has nothing to wrap here.
        handlerType.GetCustomAttribute<TransactionalAttribute>().ShouldBeNull();
    }

    private void ProductIsInTheRepository(Product product) =>
        _repository
            .GetByIdAsync(id: Arg.Any<ProductId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: product));

    private ValueTask<Result> HandleAsync(AdjustStockCommand command) =>
        new AdjustStockCommandHandler(repository: _repository, unitOfWork: _unitOfWork)
            .HandleAsync(command: command, cancellationToken: CancellationToken.None);
}
