// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Services;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Basket;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Features.Store.Catalog;

public sealed class ProductDetailServiceTests
{
    private readonly ICatalogApi _catalogApi = Substitute.For<ICatalogApi>();
    private readonly IBasketApi _basketApi = Substitute.For<IBasketApi>();
    private readonly ProductDetailService _sut;

    public ProductDetailServiceTests() => _sut = new ProductDetailService(catalogApi: _catalogApi, basketApi: _basketApi);

    [Fact]
    public async Task GetProductAsync_Should_CallTheApi_WithTheGivenId()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var product = new Product(
            Id: id,
            Sku: "SKU-1",
            Name: "Widget",
            Description: null,
            PriceAmount: 9.99m,
            PriceCurrency: "USD",
            StockQuantity: 5,
            BrandId: Guid.CreateVersion7(),
            BrandName: "Brand",
            CategoryId: Guid.CreateVersion7(),
            IsDiscontinued: false);

        _catalogApi.GetProductAsync(id: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<Product>.Success(value: product)));

        // Act
        ClientResult<Product> result = await _sut.GetProductAsync(id: id, cancellationToken: CancellationToken.None);

        // Assert
        result.Value.ShouldBe(expected: product);
        await _catalogApi.Received(requiredNumberOfCalls: 1).GetProductAsync(id: id, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddToBasketAsync_Should_MapTheProductAndQuantityIntoTheRequest()
    {
        // Arrange
        var productId = Guid.CreateVersion7();
        var basket = new BasketDto(BasketId: Guid.CreateVersion7(), CustomerId: Guid.CreateVersion7(), Items: [], TotalAmount: null, TotalCurrency: null);

        _basketApi.AddItemAsync(request: Arg.Any<AddItemToBasketRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<BasketDto>.Success(value: basket)));

        // Act
        await _sut.AddToBasketAsync(productId: productId, quantity: 2, cancellationToken: CancellationToken.None);

        // Assert
        await _basketApi.Received(requiredNumberOfCalls: 1).AddItemAsync(
            request: Arg.Is<AddItemToBasketRequest>(predicate: r => r.ProductId == productId && r.Quantity == 2),
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
