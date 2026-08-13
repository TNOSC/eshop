// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Tnosc.Lib.Application.Exceptions;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Infrastructure.External.Redis.Basket;

/// <summary>
/// <c>IBasketRepository</c> over <see cref="IConnectionMultiplexer"/> — the write side. One JSON
/// document per customer, with a sliding TTL refreshed on every write and optimistic concurrency on
/// the aggregate's <c>Version</c> enforced by a Lua script so the compare-and-set is atomic.
/// </summary>
/// <remarks>
/// Does not implement <c>IRepository&lt;Basket, BasketId&gt;</c> — see <c>IBasketRepository</c>'s own
/// remarks — so it is registered explicitly in <see cref="Extensions.ExternalExtensions"/> rather than
/// being picked up by Scrutor's repository scan.
/// </remarks>
/// <param name="connectionMultiplexer">The Redis connection, registered by <c>AddRedisClient</c> in <c>Server.Host</c>.</param>
/// <param name="options">The basket key prefix and TTL.</param>
internal sealed class RedisBasketRepository(IConnectionMultiplexer connectionMultiplexer, BasketOptions options)
    : Domain.Basket.Baskets.IBasketRepository
{
    // KEYS[1]   = the basket's Redis key.
    // ARGV[1]   = the version the caller loaded the basket at (0 for a brand-new basket).
    // ARGV[2]   = the new document to store, already carrying the incremented version.
    // ARGV[3]   = the TTL, in seconds, to (re)apply on a successful write.
    // Returns 1 on a successful compare-and-set, 0 on a version mismatch.
    private const string CompareAndSetScript = """
        local current = redis.call('GET', KEYS[1])
        if current then
            local ok, decoded = pcall(cjson.decode, current)
            if not ok or tostring(decoded['Version']) ~= tostring(ARGV[1]) then
                return 0
            end
        elseif tostring(ARGV[1]) ~= '0' then
            return 0
        end
        redis.call('SET', KEYS[1], ARGV[2], 'EX', ARGV[3])
        return 1
        """;

    /// <inheritdoc />
    public async ValueTask<BasketAggregate?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        IDatabase database = connectionMultiplexer.GetDatabase();
        string key = BasketKeys.ForCustomer(prefix: options.KeyPrefix, customerId: customerId);
        RedisValue value = await database.StringGetAsync(key: key);

        if (!value.HasValue)
        {
            return null;
        }

        BasketDocument document = JsonSerializer.Deserialize<BasketDocument>(json: value.ToString(), options: BasketSerialization.Options)
            ?? throw new InvalidOperationException(message: $"Basket document for customer {customerId} could not be deserialized.");

        return document.ToBasket();
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(BasketAggregate basket, CancellationToken cancellationToken = default)
    {
        IDatabase database = connectionMultiplexer.GetDatabase();
        string key = BasketKeys.ForCustomer(prefix: options.KeyPrefix, customerId: basket.CustomerId);
        string document = JsonSerializer.Serialize(value: basket.ToDocument(), options: BasketSerialization.Options);

        // The version this basket was loaded at (0 for a brand-new one that was never persisted) —
        // NOT basket.Version - 1: CreateFor itself counts as a transition, so a brand-new basket that
        // then has an item added before its first save already carries Version == 2 despite never
        // having touched Redis. OriginalVersion is what Basket tracked at load/creation time
        // specifically so this comparison stays correct regardless of how many transitions ran before
        // this first save.
        int expectedPreviousVersion = basket.OriginalVersion;

        RedisResult result = await database.ScriptEvaluateAsync(
            script: CompareAndSetScript,
            keys: [key],
            values:
            [
                expectedPreviousVersion.ToString(provider: CultureInfo.InvariantCulture),
                document,
                ((long)options.Ttl.TotalSeconds).ToString(provider: CultureInfo.InvariantCulture),
            ]);

        if ((int)result != 1)
        {
            throw new ConflictException(
                message: $"Basket for customer {basket.CustomerId} was modified concurrently.",
                correlationId: null,
                inner: null);
        }
    }

    /// <inheritdoc />
    public async ValueTask RemoveAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        IDatabase database = connectionMultiplexer.GetDatabase();
        string key = BasketKeys.ForCustomer(prefix: options.KeyPrefix, customerId: customerId);
        await database.KeyDeleteAsync(key: key);
    }
}
