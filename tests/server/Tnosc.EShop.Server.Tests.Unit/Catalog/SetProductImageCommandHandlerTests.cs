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
using Tnosc.EShop.Server.Application.Catalog.Commands.SetProductImage;
using Tnosc.EShop.Server.Application.Catalog.Ports;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Catalog;

/// <summary>
/// Load, upload through <see cref="IProductImageStorage"/> — deleting a previous image first — hand
/// the resulting URL to <see cref="Product.SetImage"/>, commit.
/// </summary>
public sealed class SetProductImageCommandHandlerTests
{
    private readonly Faker _faker = CatalogFaker.New();
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IProductImageStorage _imageStorage = Substitute.For<IProductImageStorage>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task HandleAsync_Should_UploadAndSetTheImageAndCommit_When_TheProductHasNoImageYet()
    {
        // Arrange
        Product product = await ProductTestFactory.CreateAsync();
        ProductIsInTheRepository(product: product);
        string uploadedUrl = _faker.Internet.Url();
        byte[] content = [1, 2, 3];
        _imageStorage
            .UploadAsync(productId: Arg.Any<Guid>(), fileName: Arg.Any<string>(), contentType: Arg.Any<string>(), content: Arg.Any<byte[]>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult(result: uploadedUrl));

        // Act
        Result result = await HandleAsync(command: new SetProductImageCommand(
            ProductId: product.Id.Value,
            FileName: "photo.jpg",
            ContentType: "image/jpeg",
            Content: content));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.ImageUrl.ShouldBe(expected: uploadedUrl);
        await _imageStorage.DidNotReceive().DeleteAsync(imageUrl: Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>());
        _repository.Received(requiredNumberOfCalls: 1).Update(aggregate: product);
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_DeleteThePreviousImage_When_TheProductAlreadyHasOne()
    {
        // Arrange
        Product product = await ProductTestFactory.CreateAsync();
        string previousUrl = _faker.Internet.Url();
        product.SetImage(imageUrl: previousUrl);
        ProductIsInTheRepository(product: product);
        string uploadedUrl = _faker.Internet.Url();
        _imageStorage
            .UploadAsync(productId: Arg.Any<Guid>(), fileName: Arg.Any<string>(), contentType: Arg.Any<string>(), content: Arg.Any<byte[]>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult(result: uploadedUrl));

        // Act
        Result result = await HandleAsync(command: new SetProductImageCommand(
            ProductId: product.Id.Value,
            FileName: "photo.jpg",
            ContentType: "image/jpeg",
            Content: [1, 2, 3]));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.ImageUrl.ShouldBe(expected: uploadedUrl);
        await _imageStorage.Received(requiredNumberOfCalls: 1).DeleteAsync(imageUrl: previousUrl, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_NoProductCarriesThatId()
    {
        // Arrange
        _repository
            .GetByIdAsync(id: Arg.Any<ProductId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: null));

        // Act
        Result result = await HandleAsync(command: new SetProductImageCommand(
            ProductId: Guid.CreateVersion7(),
            FileName: "photo.jpg",
            ContentType: "image/jpeg",
            Content: [1, 2, 3]));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Product.NotFound");
        await _imageStorage.DidNotReceive().UploadAsync(
            productId: Arg.Any<Guid>(),
            fileName: Arg.Any<string>(),
            contentType: Arg.Any<string>(),
            content: Arg.Any<byte[]>(),
            cancellationToken: Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    private void ProductIsInTheRepository(Product product) =>
        _repository
            .GetByIdAsync(id: Arg.Any<ProductId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: product));

    private ValueTask<Result> HandleAsync(SetProductImageCommand command) =>
        new SetProductImageCommandHandler(repository: _repository, imageStorage: _imageStorage, unitOfWork: _unitOfWork)
            .HandleAsync(command: command, cancellationToken: CancellationToken.None);
}
