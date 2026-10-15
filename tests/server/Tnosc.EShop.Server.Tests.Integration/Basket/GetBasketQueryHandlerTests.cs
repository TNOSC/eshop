// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.EShop.Server.Domain.Basket.Baskets;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Shared.Results;
using Xunit;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Tests.Integration.Basket;

/// <summary>
/// <c>GetBasketQueryHandler</c> against the real Redis basket store and Postgres product lookup:
/// the empty-basket shape, the line projection, and the image-URL fallback for older snapshots.
/// </summary>
[Collection(nameof(SharedInfrastructureCollection))]
public sealed class GetBasketQueryHandlerTests(PostgresFixture fixture) : BasketIntegrationTestBase(fixture)
{
    private readonly Faker _faker = BasketFaker.New();

    [Fact]
    public async Task HandleAsync_Should_ReturnAnEmptyBasket_When_TheCustomerHasNone()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();

        // Act
        Result<BasketDto> result = await QueryHandler<GetBasketQuery, BasketDto>()
            .HandleAsync(query: new GetBasketQuery(CustomerId: customerId), cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.BasketId.ShouldBe(expected: Guid.Empty);
        result.Value.CustomerId.ShouldBe(expected: customerId);
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalAmount.ShouldBeNull();
        result.Value.TotalCurrency.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_ProjectEveryLineAndTheTotal_When_TheBasketHasItems()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        string sku = _faker.Sku();
        string name = _faker.ProductName();
        Product product = await SeedProductAsync(sku: sku, name: name, amount: 12.50m, currency: "EUR");
        var basket = BasketAggregate.CreateFor(customerId: customerId);
        basket.AddItem(
            productId: product.Id.Value,
            sku: product.Sku.Value,
            productName: product.Name,
            unitPrice: product.Price,
            quantity: Quantity.Create(value: 3).Value);
        await BasketRepository.SaveAsync(basket: basket, cancellationToken: CancellationToken.None);

        // Act
        Result<BasketDto> result = await QueryHandler<GetBasketQuery, BasketDto>()
            .HandleAsync(query: new GetBasketQuery(CustomerId: customerId), cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.BasketId.ShouldBe(expected: basket.Id.Value);
        result.Value.CustomerId.ShouldBe(expected: customerId);
        result.Value.TotalAmount.ShouldBe(expected: 37.50m);
        result.Value.TotalCurrency.ShouldBe(expected: "EUR");
        BasketItemDto item = result.Value.Items.ShouldHaveSingleItem();
        item.ProductId.ShouldBe(expected: product.Id.Value);
        item.Sku.ShouldBe(expected: sku);
        item.ProductName.ShouldBe(expected: name);
        item.UnitPriceAmount.ShouldBe(expected: 12.50m);
        item.UnitPriceCurrency.ShouldBe(expected: "EUR");
        item.Quantity.ShouldBe(expected: 3);
    }

    [Fact]
    public async Task HandleAsync_Should_FillTheImageUrlFromTheProduct_When_TheStoredLineHasNone()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        Product product = await SeedProductAsync(sku: _faker.Sku(), name: _faker.ProductName(), amount: _faker.PriceAmount(), currency: "EUR");
        const string imageUrl = "https://cdn.example.com/products/basket-line.webp";
        product.SetImage(imageUrl: imageUrl).IsSuccess.ShouldBeTrue();
        Scope.ServiceProvider.GetRequiredService<IProductRepository>().Update(aggregate: product);
        await UnitOfWork.SaveChangesAsync();

        var basket = BasketAggregate.CreateFor(customerId: customerId);
        basket.AddItem(
            productId: product.Id.Value,
            sku: product.Sku.Value,
            productName: product.Name,
            unitPrice: product.Price,
            quantity: Quantity.Create(value: 1).Value);
        await BasketRepository.SaveAsync(basket: basket, cancellationToken: CancellationToken.None);

        // Act
        Result<BasketDto> result = await QueryHandler<GetBasketQuery, BasketDto>()
            .HandleAsync(query: new GetBasketQuery(CustomerId: customerId), cancellationToken: CancellationToken.None);

        // Assert
        result.Value.Items.ShouldHaveSingleItem().ImageUrl.ShouldBe(expected: imageUrl);
    }
}
