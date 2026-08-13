// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Tnosc.EShop.Server.Application.Basket.Ports;

namespace Tnosc.EShop.Server.Infrastructure.External.Redis.Basket;

/// <summary>
/// <c>IBasketReader</c> over <see cref="IConnectionMultiplexer"/> — the read side. Reads the same
/// document <see cref="RedisBasketRepository"/> writes, from the same key composed by
/// <see cref="BasketKeys"/>, so a write is visible to the very next read.
/// </summary>
/// <param name="connectionMultiplexer">The Redis connection, registered by <c>AddRedisClient</c> in <c>Server.Host</c>.</param>
/// <param name="options">The basket key prefix.</param>
internal sealed class RedisBasketReader(IConnectionMultiplexer connectionMultiplexer, BasketOptions options)
    : IBasketReader
{
    /// <inheritdoc />
    public async ValueTask<BasketSnapshot?> ReadAsync(Guid customerId, CancellationToken cancellationToken = default)
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

        decimal? totalAmount = null;
        string? totalCurrency = null;

        if (document.Items.Length > 0)
        {
            totalCurrency = document.Items[0].UnitPriceCurrency;
            totalAmount = document.Items.Sum(selector: item => item.UnitPriceAmount * item.Quantity);
        }

        return new BasketSnapshot(
            BasketId: document.BasketId,
            CustomerId: document.CustomerId,
            Items: [.. document.Items.Select(selector: static item => new BasketLineSnapshot(
                ItemId: item.ItemId,
                ProductId: item.ProductId,
                Sku: item.Sku,
                ProductName: item.ProductName,
                UnitPriceAmount: item.UnitPriceAmount,
                UnitPriceCurrency: item.UnitPriceCurrency,
                Quantity: item.Quantity))],
            TotalAmount: totalAmount,
            TotalCurrency: totalCurrency);
    }
}
