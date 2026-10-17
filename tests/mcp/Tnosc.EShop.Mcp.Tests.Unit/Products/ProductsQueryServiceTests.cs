// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Tnosc.EShop.Mcp.Application.Ports;
using Tnosc.EShop.Mcp.Application.Products;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Mcp.Tests.Unit.Products;

/// <summary>
/// <see cref="ProductsQueryService.GetProductByIdAsync"/> turns every way the eShop API can answer —
/// a product, no product, an unreachable host, an unreadable body — into a <see cref="Result{TValue}"/>.
/// </summary>
public sealed class ProductsQueryServiceTests
{
    private readonly IEShopClient _eShopClient = Substitute.For<IEShopClient>();
    private readonly Guid _productId = Guid.CreateVersion7();

    [Fact]
    public async Task GetProductByIdAsync_Should_ReturnTheProduct_When_TheClientFindsIt()
    {
        // Arrange
        ProductDetails product = ProductDetailsFactory.Create(id: _productId);
        _eShopClient
            .GetProductByIdAsync(productId: _productId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: product);

        // Act
        Result<ProductDetails> result = await CreateService().GetProductByIdAsync(productId: _productId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected: product);
    }

    [Fact]
    public async Task GetProductByIdAsync_Should_ReturnNotFound_When_TheClientReturnsNull()
    {
        // Arrange
        _eShopClient
            .GetProductByIdAsync(productId: _productId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: (ProductDetails?)null);

        // Act
        Result<ProductDetails> result = await CreateService().GetProductByIdAsync(productId: _productId);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Products.NotFound");
    }

    [Fact]
    public async Task GetProductByIdAsync_Should_ReturnUnavailable_When_TheCatalogCannotBeReached()
    {
        // Arrange
        _eShopClient
            .GetProductByIdAsync(productId: _productId, cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(ex: new HttpRequestException(message: "connection refused"));

        // Act
        Result<ProductDetails> result = await CreateService().GetProductByIdAsync(productId: _productId);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Failure);
        result.FirstError.Code.ShouldBe(expected: "Products.Unavailable");
    }

    [Fact]
    public async Task GetProductByIdAsync_Should_ReturnInvalidResponse_When_TheBodyCannotBeRead()
    {
        // Arrange
        _eShopClient
            .GetProductByIdAsync(productId: _productId, cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(ex: new JsonException(message: "unexpected token"));

        // Act
        Result<ProductDetails> result = await CreateService().GetProductByIdAsync(productId: _productId);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Unexpected);
        result.FirstError.Code.ShouldBe(expected: "Products.InvalidResponse");
    }

    private ProductsQueryService CreateService() => new(eShopClient: _eShopClient);
}
