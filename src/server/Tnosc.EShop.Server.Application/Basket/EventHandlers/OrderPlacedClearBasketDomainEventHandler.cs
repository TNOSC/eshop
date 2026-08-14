// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Basket.Commands.ClearBasket;
using Tnosc.EShop.Server.Domain.Ordering.Orders.Events;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Basket.EventHandlers;

/// <summary>
/// Empties a customer's basket once their order has been placed.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Basket's handler, in Basket's folder, over Basket's own aggregate.</strong> It reads one
/// field of Ordering's published event and touches nothing of Ordering's — the only coupling the
/// design doc sanctions between two bounded contexts, and the reason the outbox exists.
/// </para>
/// <para>
/// <strong>Deliberately not <c>[Idempotent]</c>.</strong> Clearing a basket is a Redis delete, which
/// is already idempotent by construction — running it twice leaves exactly the state running it once
/// does. Marking it would be worse than useless: <c>IdempotencyDecorator</c> commits its claim in an
/// EF transaction that the Redis delete does not join, so a crash between the two would burn the key
/// while leaving the basket full, and the redelivery that exists to fix that would be skipped. The
/// attribute would open precisely the window it exists to close.
/// </para>
/// <para>
/// It delegates to <see cref="ClearBasketCommand"/> rather than calling
/// <c>IBasketRepository</c> itself, for one concrete reason: <c>GetBasketQueryHandler</c> is
/// <c>[Cacheable(60)]</c>, and only the <em>command</em> pipeline carries
/// <c>CacheInvalidationDecorator</c>. Going through the command runs that decorator and evicts the
/// <c>basket</c> tag; going straight to the repository would clear the store and leave the customer
/// looking at a cached copy of the basket they just ordered.
/// </para>
/// </remarks>
/// <param name="clearBasket">The Basket command handler that owns emptying a basket.</param>
internal sealed class OrderPlacedClearBasketDomainEventHandler(ICommandHandler<ClearBasketCommand> clearBasket)
    : IDomainEventHandler<OrderPlacedDomainEvent>
{
    /// <inheritdoc />
    public async ValueTask HandleAsync(OrderPlacedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: @event);

        Result cleared = await clearBasket.HandleAsync(
            command: new ClearBasketCommand(CustomerId: @event.CustomerId),
            cancellationToken: cancellationToken);

        // The command pipeline turns a failure into a Result rather than letting it escape, so a
        // silent `return` here would mark the outbox row processed for work that did not happen.
        // Throwing hands the row back to the processor, which retries it and eventually dead-letters it.
        if (cleared.IsError)
        {
            throw new InvalidOperationException(
                message: $"Clearing the basket of customer {@event.CustomerId} after order {@event.OrderNumber} failed: {cleared.FirstError.Code} — {cleared.FirstError.Description}.");
        }
    }
}
