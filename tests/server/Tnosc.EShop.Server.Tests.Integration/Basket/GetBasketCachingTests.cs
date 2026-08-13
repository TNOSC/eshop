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
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tnosc.EShop.Server.Application.Basket.Commands.AddItemToBasket;
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.EShop.Server.Domain.Basket.Baskets;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Xunit;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Tests.Integration.Basket;

/// <summary>
/// The two halves of the cache contract for Basket, now through the Redis-backed L1+L2
/// <c>HybridCache</c> pair: <c>[Cacheable(60)]</c> on <see cref="GetBasketQueryHandler"/> serves the
/// second call without touching the Redis basket store again, and <c>[CacheTag("basket")]</c> on a
/// write handler drops that entry.
/// </summary>
[Collection(nameof(PostgresCollection))]
public sealed class GetBasketCachingTests(PostgresFixture fixture) : BasketIntegrationTestBase(fixture)
{
    private readonly Faker _faker = BasketFaker.New();

    [Fact]
    public async Task GetBasket_Should_ServeTheSecondCallFromCache()
    {
        // Arrange
        await ResetBasketCacheAsync();
        Guid customerId = _faker.CustomerId();
        Product product = await SeedProductAsync(sku: _faker.Sku(), name: _faker.ProductName(), amount: _faker.PriceAmount(), currency: "EUR");
        var basket = BasketAggregate.CreateFor(customerId: customerId);
        basket.AddItem(productId: product.Id.Value, sku: product.Sku.Value, productName: product.Name, unitPrice: product.Price, quantity: Quantity.Create(value: 1).Value);
        await BasketRepository.SaveAsync(basket: basket, cancellationToken: CancellationToken.None);

        // Act
        Result<BasketDto> first = await GetBasketAsync(customerId: customerId);

        // A write that bypasses the command pipeline, so nothing invalidates the tag.
        BasketAggregate reloaded = (await BasketRepository.GetByCustomerIdAsync(customerId: customerId, cancellationToken: CancellationToken.None))!;
        reloaded.Clear();
        await BasketRepository.SaveAsync(basket: reloaded, cancellationToken: CancellationToken.None);

        Result<BasketDto> second = await GetBasketAsync(customerId: customerId);

        // Assert
        first.Value.Items.ShouldHaveSingleItem();
        second.Value.Items.ShouldHaveSingleItem(customMessage: "the second call must come from cache, not from Redis");
    }

    [Fact]
    public async Task AddItemToBasket_Should_InvalidateTheCachedGetBasket()
    {
        // Arrange
        await ResetBasketCacheAsync();
        Guid customerId = _faker.CustomerId();
        Product product = await SeedProductAsync(sku: _faker.Sku(), name: _faker.ProductName(), amount: _faker.PriceAmount(), currency: "EUR");

        // Populates the cache entry under the "basket" tag with an empty basket.
        Result<BasketDto> beforeAdd = await GetBasketAsync(customerId: customerId);
        beforeAdd.Value.Items.ShouldBeEmpty();

        // Act — AddItemToBasketCommandHandler carries [CacheTag("basket")], so a successful command
        // runs CacheInvalidationDecorator and drops the entry.
        Result<BasketDto> added = await Scope.ServiceProvider.GetRequiredService<ICommandHandler<AddItemToBasketCommand, BasketDto>>()
            .HandleAsync(
                command: new AddItemToBasketCommand(CustomerId: customerId, ProductId: product.Id.Value, Quantity: 1),
                cancellationToken: CancellationToken.None);

        // Assert
        added.IsSuccess.ShouldBeTrue();

        Result<BasketDto> afterAdd = await GetBasketAsync(customerId: customerId);
        afterAdd.Value.Items.ShouldHaveSingleItem(customMessage: "a successful basket command must invalidate the basket cache tag");
        afterAdd.Value.Items.Single().ProductId.ShouldBe(expected: product.Id.Value);
    }

    /// <summary>
    /// Drops any "basket" entry an earlier run left behind. Mirrors
    /// <c>GetCategoriesCachingTests.ResetCatalogCacheAsync</c> — see its remarks for why both clock
    /// moves are load-bearing.
    /// </summary>
    private async Task ResetBasketCacheAsync()
    {
        TimeProvider.Advance(delta: TimeSpan.FromMinutes(value: 10));

        await Scope.ServiceProvider.GetRequiredService<HybridCache>()
            .RemoveByTagAsync(tag: "basket", cancellationToken: CancellationToken.None);

        TimeProvider.Advance(delta: TimeSpan.FromSeconds(value: 1));
    }

    private ValueTask<Result<BasketDto>> GetBasketAsync(Guid customerId) =>
        QueryHandler<GetBasketQuery, BasketDto>().HandleAsync(
            query: new GetBasketQuery(CustomerId: customerId),
            cancellationToken: CancellationToken.None);
}
