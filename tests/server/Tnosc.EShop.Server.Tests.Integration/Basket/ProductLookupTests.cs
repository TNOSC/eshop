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
using Tnosc.EShop.Server.Application.Basket.Ports;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Basket;

/// <summary>
/// <c>ProductLookup</c> against real Postgres data — the one genuine database read Basket's
/// infrastructure performs, kept in <c>Server.Infrastructure.Persistence</c> rather than moving to
/// <c>Server.Infrastructure.External</c> alongside the rest of Basket's plumbing.
/// </summary>
[Collection(nameof(SharedInfrastructureCollection))]
public sealed class ProductLookupTests(PostgresFixture fixture) : BasketIntegrationTestBase(fixture)
{
    private readonly Faker _faker = BasketFaker.New();

    [Fact]
    public async Task GetAsync_Should_ReturnTheProductsSnapshotData_When_TheProductExists()
    {
        // Arrange
        string sku = _faker.Sku();
        string name = _faker.ProductName();
        decimal amount = _faker.PriceAmount();
        Product product = await SeedProductAsync(sku: sku, name: name, amount: amount, currency: "EUR");

        // Act
        ProductSnapshot? snapshot = await ProductLookup.GetAsync(productId: product.Id.Value, cancellationToken: CancellationToken.None);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.ProductId.ShouldBe(expected: product.Id.Value);
        snapshot.Sku.ShouldBe(expected: sku);
        snapshot.Name.ShouldBe(expected: name);
        snapshot.PriceAmount.ShouldBe(expected: amount);
        snapshot.PriceCurrency.ShouldBe(expected: "EUR");
    }

    [Fact]
    public async Task GetAsync_Should_ReturnNull_When_NoProductCarriesThatId()
    {
        // Act
        ProductSnapshot? snapshot = await ProductLookup.GetAsync(productId: Guid.CreateVersion7(), cancellationToken: CancellationToken.None);

        // Assert
        snapshot.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_Should_ReflectALaterPriceChange_Without_TouchingAnyExistingBasketLine()
    {
        // Arrange — this is the whole point of the snapshot: a later catalogue price change must not
        // silently rewrite a basket line that already captured the old price.
        Product product = await SeedProductAsync(sku: _faker.Sku(), name: _faker.ProductName(), amount: 10.00m, currency: "EUR");
        ProductSnapshot? snapshotBeforeChange = await ProductLookup.GetAsync(productId: product.Id.Value, cancellationToken: CancellationToken.None);

        product.ChangePrice(newPrice: Money.Create(amount: 25.00m, currency: "EUR").Value);
        Scope.ServiceProvider.GetRequiredService<IProductRepository>().Update(aggregate: product);
        await UnitOfWork.SaveChangesAsync();

        // Act
        ProductSnapshot? snapshotAfterChange = await ProductLookup.GetAsync(productId: product.Id.Value, cancellationToken: CancellationToken.None);

        // Assert
        snapshotBeforeChange.ShouldNotBeNull();
        snapshotBeforeChange.PriceAmount.ShouldBe(expected: 10.00m);
        snapshotAfterChange.ShouldNotBeNull();
        snapshotAfterChange.PriceAmount.ShouldBe(expected: 25.00m, customMessage: "the live lookup reflects the new price — the basket line that already snapshotted the old one is what stays unaffected");
    }
}
