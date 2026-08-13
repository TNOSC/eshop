// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Basket.Ports;
using Tnosc.EShop.Server.Application.Ordering.Ports;

namespace Tnosc.EShop.Server.Infrastructure.External.Redis.Ordering;

/// <summary>
/// Implements Ordering's <see cref="IOrderBasketReader"/> on top of Basket's own
/// <see cref="IBasketReader"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This class is the entire coupling between Ordering and Basket, and it is an adapter, which
/// is what adapters are for.</strong> Neither context's application code names a type of the other's:
/// <c>BasketResolver</c> depends on <see cref="IOrderBasketReader"/> and sees
/// <see cref="OrderBasketSnapshot"/>, Basket's handlers depend on <see cref="IBasketReader"/> and see
/// <c>BasketSnapshot</c>, and the mapping between the two shapes happens here in infrastructure.
/// </para>
/// <para>
/// Delegating to <see cref="IBasketReader"/> rather than reading Redis directly is deliberate. A
/// second reader would have to duplicate the key format and the document schema, and the copy would
/// drift the first time Basket changed either — silently, because Ordering would simply start seeing
/// no basket. Sharing the read port means a change to the stored shape breaks in one place, at
/// compile time.
/// </para>
/// </remarks>
/// <param name="basketReader">Basket's read port over Redis.</param>
internal sealed class RedisOrderBasketReader(IBasketReader basketReader) : IOrderBasketReader
{
    /// <inheritdoc />
    public async ValueTask<OrderBasketSnapshot?> ReadAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        BasketSnapshot? basket = await basketReader.ReadAsync(
            customerId: customerId,
            cancellationToken: cancellationToken);

        if (basket is null)
        {
            return null;
        }

        return new OrderBasketSnapshot(
            CustomerId: basket.CustomerId,
            Lines: [.. basket.Items.Select(selector: static item => new OrderBasketLine(
                ProductId: item.ProductId,
                Sku: item.Sku,
                ProductName: item.ProductName,
                UnitPriceAmount: item.UnitPriceAmount,
                UnitPriceCurrency: item.UnitPriceCurrency,
                Quantity: item.Quantity))]);
    }
}
