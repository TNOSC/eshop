// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Mcp.Application.Products;
using Tnosc.EShop.Mcp.Tool;
using Tnosc.EShop.Mcp.Tool.Products;
using Tnosc.EShop.Server.Shared.Catalog;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Mcp.Tests.Unit.Products;

/// <summary>
/// <see cref="ProductsTool.GetProductAsync"/> validates its own input, delegates to
/// <see cref="IProductsQueryService"/>, and reports every outcome through <see cref="ToolResult{TValue}"/>.
/// </summary>
public sealed class ProductsToolTests
{
    private readonly IProductsQueryService _productsQueryService = Substitute.For<IProductsQueryService>();
    private readonly Guid _productId = Guid.CreateVersion7();

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("12345")]
    public async Task GetProductAsync_Should_Fail_Without_CallingTheService_When_ProductIdIsNotAGuid(string productId)
    {
        // Act
        ToolResult<ProductDetails> result = await ProductsTool.GetProductAsync(
            productsQueryService: _productsQueryService,
            productId: productId,
            cancellationToken: CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.Data.ShouldBeNull();
        result.ErrorCode.ShouldBe(expected: "Products.InvalidProductId");
        await _productsQueryService.DidNotReceiveWithAnyArgs().GetProductByIdAsync(
            productId: Arg.Any<Guid>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProductAsync_Should_ReturnTheProduct_When_TheServiceFindsIt()
    {
        // Arrange
        ProductDetails product = ProductDetailsFactory.Create(id: _productId);
        _productsQueryService
            .GetProductByIdAsync(productId: _productId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Result<ProductDetails>.Success(value: product));

        // Act
        ToolResult<ProductDetails> result = await ProductsTool.GetProductAsync(
            productsQueryService: _productsQueryService,
            productId: _productId.ToString(),
            cancellationToken: CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldBe(expected: product);
        result.ErrorCode.ShouldBeNull();
    }

    [Fact]
    public async Task GetProductAsync_Should_ReportTheServiceError_When_TheProductDoesNotExist()
    {
        // Arrange
        _productsQueryService
            .GetProductByIdAsync(productId: _productId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Error.NotFound(code: "Products.NotFound", description: "No such product."));

        // Act
        ToolResult<ProductDetails> result = await ProductsTool.GetProductAsync(
            productsQueryService: _productsQueryService,
            productId: _productId.ToString(),
            cancellationToken: CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.Data.ShouldBeNull();
        result.ErrorCode.ShouldBe(expected: "Products.NotFound");
        result.ErrorMessage.ShouldBe(expected: "No such product.");
    }

    [Fact]
    public void GetProductAsync_Should_BeExposed_Under_TheSharedToolName()
    {
        // An agent's allow-list filters on this protocol name; an SDK-derived name would silently
        // match nothing, so the declared name must be the shared constant.

        // Arrange
        MethodInfo method = typeof(ProductsTool).GetMethod(name: nameof(ProductsTool.GetProductAsync))!;

        // Act
        McpServerToolAttribute? attribute = method.GetCustomAttribute<McpServerToolAttribute>();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.Name.ShouldBe(expected: McpToolNames.GetProduct);
    }
}
