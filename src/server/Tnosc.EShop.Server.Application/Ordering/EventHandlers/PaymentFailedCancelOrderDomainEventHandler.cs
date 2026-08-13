// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Domain.Payment.Payments.Events;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Ordering.EventHandlers;

/// <summary>
/// Cancels an order once its payment has failed.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Ordering's handler, in Ordering's folder, over Ordering's own aggregate.</strong> Unlike
/// <c>PaymentSucceededMarkOrderPaidDomainEventHandler</c>, <c>Order.Cancel()</c> is reachable from every
/// pre-despatch status — a declined payment cancels the order whether or not it had already been
/// confirmed.
/// </para>
/// <para>
/// <strong><c>[Idempotent]</c> is load-bearing</strong> for the same reason as the succeeded sibling:
/// without it a redelivered <c>PaymentFailed</c> would attempt <c>Cancel()</c> on an order already
/// cancelled — an illegal transition this handler would otherwise dead-letter as though it were a
/// genuine failure.
/// </para>
/// </remarks>
/// <param name="repository">The order repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
[Idempotent]
internal sealed class PaymentFailedCancelOrderDomainEventHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork)
    : IDomainEventHandler<PaymentFailedDomainEvent>
{
    /// <inheritdoc />
    public async ValueTask HandleAsync(PaymentFailedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: @event);

        Order order = await repository.GetByIdAsync(
                id: OrderId.From(value: @event.OrderId),
                cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException(
                message: $"Payment {@event.PaymentId} failed for order {@event.OrderId}, which no longer exists.");

        Result cancelled = order.Cancel();

        if (cancelled.IsError)
        {
            throw new InvalidOperationException(
                message: $"Cancelling order {@event.OrderId} after payment {@event.PaymentId} failed failed: {cancelled.FirstError.Code} — {cancelled.FirstError.Description}.");
        }

        repository.Update(aggregate: order);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
    }
}
