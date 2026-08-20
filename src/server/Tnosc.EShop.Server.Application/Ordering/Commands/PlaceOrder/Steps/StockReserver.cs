// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Ordering.Ports;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder.Steps;

/// <summary>
/// Fails the order early when the catalogue cannot supply a line.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This step checks; it does not decrement.</strong> The decrement is Catalog's own
/// <c>OrderPlacedAdjustStockDomainEventHandler</c>, driven by <c>OrderPlacedDomainEvent</c> through
/// the outbox — and it has to be, for two reasons. Ordering must not write to Catalog's aggregates,
/// and the outbox handler is <c>[Idempotent]</c>, so an at-least-once redelivery decrements once.
/// Decrementing here as well would take the units off twice for every order.
/// </para>
/// <para>
/// The consequence, stated plainly: this is an optimistic check, not a lock. Two orders placed
/// concurrently for the last unit can both pass it, and the second is caught when
/// <c>Product.AdjustStock</c> refuses to go below zero — at which point the message dead-letters for
/// an operator rather than silently overselling. Holding a real reservation needs a reservation
/// aggregate with its own expiry, which is a larger design than this slice, and the check earns its
/// place regardless: it turns the overwhelmingly common case, ordering something that is plainly out
/// of stock, into a 409 at request time instead of a dead letter an hour later.
/// </para>
/// </remarks>
/// <param name="stockReader">The catalogue stock read port.</param>
internal sealed class StockReserver(IStockAvailabilityReader stockReader) : IStockReserver
{
    /// <inheritdoc />
    public async ValueTask<Result> ReserveAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: order);

        List<Guid> productIds = [.. order.Lines.Select(selector: static line => line.ProductId).Distinct()];

        IReadOnlyDictionary<Guid, int> stockLevels = await stockReader.GetStockLevelsAsync(
            productIds: productIds,
            cancellationToken: cancellationToken);

        foreach (OrderLine line in order.Lines)
        {
            // A product missing from the result no longer exists in the catalogue, which is the same
            // problem as having none on hand and is reported the same way.
            stockLevels.TryGetValue(key: line.ProductId, value: out int available);

            if (available < line.Quantity.Value)
            {
                return OrderErrors.InsufficientStock(
                    productId: line.ProductId,
                    requested: line.Quantity.Value,
                    available: available);
            }
        }

        return Result.Success();
    }
}
