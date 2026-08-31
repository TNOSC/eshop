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
using Tnosc.EShop.Server.Application.Catalog.Commands.RemoveProductImage;
using Tnosc.EShop.Server.Application.Catalog.Ports;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Catalog;

/// <summary>
/// Load, delete the blob through <see cref="IProductImageStorage"/> (if any), hand the transition to
/// <see cref="Product.ClearImage"/>, commit.
/// </summary>
public sealed class RemoveProductImageCommandHandlerTests
{
    private readonly Faker _faker = CatalogFaker.New();
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IProductImageStorage _imageStorage = Substitute.For<IProductImageStorage>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task HandleAsync_Should_DeleteTheBlobAndClearTheImageAndCommit_When_TheProductHasAnImage()
    {
        // Arrange
        Product product = await ProductTestFactory.CreateAsync();
        string imageUrl = _faker.Internet.Url();
        product.SetImage(imageUrl: imageUrl);
        ProductIsInTheRepository(product: product);

        // Act
        Result result = await HandleAsync(command: new RemoveProductImageCommand(ProductId: product.Id.Value));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.ImageUrl.ShouldBeNull();
        await _imageStorage.Received(requiredNumberOfCalls: 1).DeleteAsync(imageUrl: imageUrl, cancellationToken: Arg.Any<CancellationToken>());
        _repository.Received(requiredNumberOfCalls: 1).Update(aggregate: product);
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_BeANoOpSuccess_When_TheProductHasNoImage()
    {
        // Arrange
        Product product = await ProductTestFactory.CreateAsync();
        ProductIsInTheRepository(product: product);

        // Act
        Result result = await HandleAsync(command: new RemoveProductImageCommand(ProductId: product.Id.Value));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _imageStorage.DidNotReceive().DeleteAsync(imageUrl: Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_NoProductCarriesThatId()
    {
        // Arrange
        _repository
            .GetByIdAsync(id: Arg.Any<ProductId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: null));

        // Act
        Result result = await HandleAsync(command: new RemoveProductImageCommand(ProductId: Guid.CreateVersion7()));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Product.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    private void ProductIsInTheRepository(Product product) =>
        _repository
            .GetByIdAsync(id: Arg.Any<ProductId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: product));

    private ValueTask<Result> HandleAsync(RemoveProductImageCommand command) =>
        new RemoveProductImageCommandHandler(repository: _repository, imageStorage: _imageStorage, unitOfWork: _unitOfWork)
            .HandleAsync(command: command, cancellationToken: CancellationToken.None);
}
