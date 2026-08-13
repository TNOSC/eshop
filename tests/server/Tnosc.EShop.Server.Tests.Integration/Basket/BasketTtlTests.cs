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
using StackExchange.Redis;
using Tnosc.EShop.Server.Infrastructure.External.Redis.Basket;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Xunit;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Tests.Integration.Basket;

/// <summary>
/// A basket document carries a sliding TTL: set on the first write, and refreshed by every
/// subsequent one.
/// </summary>
[Collection(nameof(PostgresCollection))]
public sealed class BasketTtlTests(PostgresFixture fixture) : BasketIntegrationTestBase(fixture)
{
    private readonly Faker _faker = BasketFaker.New();

    [Fact]
    public async Task SaveAsync_Should_SetATtl_OnTheFirstWrite()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        var basket = BasketAggregate.CreateFor(customerId: customerId);
        BasketOptions options = Scope.ServiceProvider.GetRequiredService<BasketOptions>();
        string key = BasketKeys.ForCustomer(prefix: options.KeyPrefix, customerId: customerId);

        // Act
        await BasketRepository.SaveAsync(basket: basket, cancellationToken: CancellationToken.None);
        TimeSpan? ttl = await RedisConnection.GetDatabase().KeyTimeToLiveAsync(key: key);

        // Assert
        ttl.ShouldNotBeNull();
        ttl.Value.ShouldBeGreaterThan(expected: TimeSpan.Zero);
        ttl.Value.ShouldBeLessThanOrEqualTo(expected: options.Ttl);
    }

    [Fact]
    public async Task SaveAsync_Should_RefreshTheTtl_OnASubsequentWrite()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        var basket = BasketAggregate.CreateFor(customerId: customerId);
        BasketOptions options = Scope.ServiceProvider.GetRequiredService<BasketOptions>();
        string key = BasketKeys.ForCustomer(prefix: options.KeyPrefix, customerId: customerId);

        await BasketRepository.SaveAsync(basket: basket, cancellationToken: CancellationToken.None);
        IDatabase database = RedisConnection.GetDatabase();

        // Force the TTL down, below what a fresh write would set, so a refresh is observable.
        await database.KeyExpireAsync(key: key, expiry: TimeSpan.FromSeconds(value: 5));

        // Act
        BasketAggregate reloaded = (await BasketRepository.GetByCustomerIdAsync(customerId: customerId, cancellationToken: CancellationToken.None))!;
        reloaded.Clear();
        await BasketRepository.SaveAsync(basket: reloaded, cancellationToken: CancellationToken.None);
        TimeSpan? ttlAfterSecondWrite = await database.KeyTimeToLiveAsync(key: key);

        // Assert
        ttlAfterSecondWrite.ShouldNotBeNull();
        ttlAfterSecondWrite.Value.ShouldBeGreaterThan(expected: TimeSpan.FromSeconds(value: 30), customMessage: "a later write must refresh the sliding TTL back up, not leave the shortened one in place");
    }
}
