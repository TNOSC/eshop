// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Shouldly;
using Tnosc.EShop.Server.Domain.Basket.Baskets;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Application.Exceptions;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Xunit;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Tests.Integration.Basket;

/// <summary>
/// <see cref="Server.Infrastructure.External.Redis.Basket.RedisBasketRepository"/> against a
/// real Redis container: add → read → re-add increments rather than duplicating, a stale version is
/// rejected as a conflict, and the repository and <see cref="Server.Infrastructure.External.Redis.Basket.RedisBasketReader"/>
/// agree on the same key so a write is visible to the very next read.
/// </summary>
[Collection(nameof(SharedInfrastructureCollection))]
public sealed class RedisBasketRepositoryTests(PostgresFixture fixture) : BasketIntegrationTestBase(fixture)
{
    private readonly Faker _faker = BasketFaker.New();

    [Fact]
    public async Task GetByCustomerIdAsync_Should_ReturnNull_When_TheCustomerHasNoBasket()
    {
        // Act
        BasketAggregate? basket = await BasketRepository.GetByCustomerIdAsync(
            customerId: _faker.CustomerId(),
            cancellationToken: CancellationToken.None);

        // Assert
        basket.ShouldBeNull();
    }

    [Fact]
    public async Task SaveAsync_Then_GetByCustomerIdAsync_Should_RoundTripTheBasket()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        Product product = await SeedProductAsync(sku: _faker.Sku(), name: _faker.ProductName(), amount: _faker.PriceAmount(), currency: "EUR");
        var basket = BasketAggregate.CreateFor(customerId: customerId);
        basket.AddItem(
            productId: product.Id.Value,
            sku: product.Sku.Value,
            productName: product.Name,
            unitPrice: product.Price,
            quantity: Quantity.Create(value: 2).Value);

        // Act
        await BasketRepository.SaveAsync(basket: basket, cancellationToken: CancellationToken.None);
        BasketAggregate? read = await BasketRepository.GetByCustomerIdAsync(customerId: customerId, cancellationToken: CancellationToken.None);

        // Assert
        read.ShouldNotBeNull();
        read.CustomerId.ShouldBe(expected: customerId);
        read.Items.ShouldHaveSingleItem();
        read.Items.Single().ProductId.ShouldBe(expected: product.Id.Value);
        read.Items.Single().Quantity.Value.ShouldBe(expected: 2);
        read.Version.ShouldBe(expected: basket.Version);
    }

    [Fact]
    public async Task AddingTheSameProductTwice_Should_IncrementTheStoredLine_Rather_ThanDuplicatingIt()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        Product product = await SeedProductAsync(sku: _faker.Sku(), name: _faker.ProductName(), amount: _faker.PriceAmount(), currency: "EUR");

        var first = BasketAggregate.CreateFor(customerId: customerId);
        first.AddItem(productId: product.Id.Value, sku: product.Sku.Value, productName: product.Name, unitPrice: product.Price, quantity: Quantity.Create(value: 1).Value);
        await BasketRepository.SaveAsync(basket: first, cancellationToken: CancellationToken.None);

        // Act — reload (as the command handler would), add the same product again, save.
        BasketAggregate reloaded = (await BasketRepository.GetByCustomerIdAsync(customerId: customerId, cancellationToken: CancellationToken.None))!;
        reloaded.AddItem(productId: product.Id.Value, sku: product.Sku.Value, productName: product.Name, unitPrice: product.Price, quantity: Quantity.Create(value: 3).Value);
        await BasketRepository.SaveAsync(basket: reloaded, cancellationToken: CancellationToken.None);

        // Assert
        BasketAggregate final = (await BasketRepository.GetByCustomerIdAsync(customerId: customerId, cancellationToken: CancellationToken.None))!;
        final.Items.ShouldHaveSingleItem(customMessage: "re-adding the same product must increment the stored line, not duplicate it");
        final.Items.Single().Quantity.Value.ShouldBe(expected: 4);
    }

    [Fact]
    public async Task SaveAsync_Should_ThrowConflictException_When_TheStoredVersionHasMovedOn()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        var basket = BasketAggregate.CreateFor(customerId: customerId);
        await BasketRepository.SaveAsync(basket: basket, cancellationToken: CancellationToken.None);

        // Simulate a concurrent writer: reload and save independently, moving the stored version on.
        BasketAggregate concurrentWriter = (await BasketRepository.GetByCustomerIdAsync(customerId: customerId, cancellationToken: CancellationToken.None))!;
        concurrentWriter.Clear();
        await BasketRepository.SaveAsync(basket: concurrentWriter, cancellationToken: CancellationToken.None);

        // Act — "basket" is still at the version it was loaded at before the concurrent write landed.
        basket.Clear();
        Func<Task> act = async () => await BasketRepository.SaveAsync(basket: basket, cancellationToken: CancellationToken.None);

        // Assert
        await act.ShouldThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task RepositoryWrite_Should_BeVisibleToTheReaderImmediately()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        Product product = await SeedProductAsync(sku: _faker.Sku(), name: _faker.ProductName(), amount: _faker.PriceAmount(), currency: "EUR");
        var basket = BasketAggregate.CreateFor(customerId: customerId);
        basket.AddItem(productId: product.Id.Value, sku: product.Sku.Value, productName: product.Name, unitPrice: product.Price, quantity: Quantity.Create(value: 1).Value);

        // Act
        await BasketRepository.SaveAsync(basket: basket, cancellationToken: CancellationToken.None);
        Tnosc.EShop.Server.Application.Basket.Ports.BasketSnapshot? snapshot = await BasketReader.ReadAsync(customerId: customerId, cancellationToken: CancellationToken.None);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.CustomerId.ShouldBe(expected: customerId);
        snapshot.Items.ShouldHaveSingleItem();
        snapshot.Items[0].ProductId.ShouldBe(expected: product.Id.Value);
    }

    [Fact]
    public async Task RemoveAsync_Should_DeleteTheStoredBasket()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        var basket = BasketAggregate.CreateFor(customerId: customerId);
        await BasketRepository.SaveAsync(basket: basket, cancellationToken: CancellationToken.None);

        // Act
        await BasketRepository.RemoveAsync(customerId: customerId, cancellationToken: CancellationToken.None);

        // Assert
        BasketAggregate? afterRemoval = await BasketRepository.GetByCustomerIdAsync(customerId: customerId, cancellationToken: CancellationToken.None);
        afterRemoval.ShouldBeNull();
    }
}
