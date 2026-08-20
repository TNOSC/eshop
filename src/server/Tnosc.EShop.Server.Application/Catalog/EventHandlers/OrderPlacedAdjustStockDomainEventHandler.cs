// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Ordering.Orders.Events;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Catalog.EventHandlers;

/// <summary>
/// Takes the ordered units off catalogue stock once an order has been placed.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Catalog's handler, in Catalog's folder, over Catalog's own aggregate.</strong> It reads
/// Ordering's published event and never touches an <c>Order</c>; Ordering, for its part, never writes
/// to a <c>Product</c>. That is the whole of the coupling between the two contexts.
/// </para>
/// <para>
/// <strong><c>[Idempotent]</c> is load-bearing here, unlike on the basket-clearing sibling.</strong>
/// Outbox delivery is at-least-once, and decrementing stock is the textbook non-idempotent effect:
/// a redelivery without the inbox would take the units off a second time and quietly corrupt the
/// stock level of every product on the order. The decorator claims
/// <c>OrderPlacedDomainEvent.Id</c> for this handler in the same transaction as the decrement below,
/// so the claim and the effect commit together or not at all, and a replay is skipped rather than
/// applied. <c>PlaceOrderOutboxTests</c> proves it by genuinely redelivering the event.
/// </para>
/// <para>
/// Note what is <em>not</em> here: no dedupe branch, no "have I seen this event" lookup, no key
/// parameter. The handler is written as though delivery were exactly-once, which is the point of the
/// attribute.
/// </para>
/// </remarks>
/// <param name="repository">The product repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
[Idempotent]
internal sealed class OrderPlacedAdjustStockDomainEventHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork)
    : IDomainEventHandler<OrderPlacedDomainEvent>
{
    /// <inheritdoc />
    public async ValueTask HandleAsync(OrderPlacedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: @event);

        foreach (OrderPlacedLine line in @event.Lines)
        {
            Product product = await repository.GetByIdAsync(
                    id: ProductId.From(value: line.ProductId),
                    cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException(
                    message: $"Order {@event.OrderNumber} names product {line.ProductId}, which is no longer in the catalogue.");

            Result adjusted = product.AdjustStock(delta: -line.Quantity);

            // The only way this fails is the oversell StockReserver's optimistic check cannot rule out:
            // two orders passed the check for the same last units. Throwing dead-letters the message for
            // an operator, which is the honest outcome — the alternative is a product silently sitting at
            // a stock level that does not match what has been sold.
            if (adjusted.IsError)
            {
                throw new InvalidOperationException(
                    message: $"Order {@event.OrderNumber} cannot take {line.Quantity} units of product {line.ProductId} off stock: {adjusted.FirstError.Code} — {adjusted.FirstError.Description}.");
            }

            repository.Update(aggregate: product);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
    }
}
