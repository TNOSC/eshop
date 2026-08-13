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
/// Marks an order paid once its payment has been captured.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Ordering's handler, in Ordering's folder, over Ordering's own aggregate.</strong> It reads
/// Payment's published event and never touches a <c>Payment</c>; Payment, for its part, never writes
/// to an <c>Order</c>.
/// </para>
/// <para>
/// <c>Order.MarkPaid()</c> is only legal from <see cref="OrderStatus.Confirmed"/> — this handler
/// assumes the order was confirmed before its payment settled, and simply propagates whatever the
/// aggregate decides. It never reads or compares <c>Status</c> itself.
/// </para>
/// <para>
/// <strong><c>[Idempotent]</c> is load-bearing.</strong> A redelivered <c>PaymentSucceeded</c> without
/// it would attempt <c>MarkPaid()</c> a second time — on an order already <c>Paid</c>, an illegal
/// transition the aggregate would reject as a <c>Conflict</c>, which this handler would then (wrongly)
/// treat as a genuine failure and dead-letter. The decorator's claim on
/// <see cref="PaymentSucceededDomainEvent.Id"/> makes a replay a no-op instead.
/// </para>
/// </remarks>
/// <param name="repository">The order repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
[Idempotent]
internal sealed class PaymentSucceededMarkOrderPaidDomainEventHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork)
    : IDomainEventHandler<PaymentSucceededDomainEvent>
{
    /// <inheritdoc />
    public async ValueTask HandleAsync(PaymentSucceededDomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: @event);

        Order order = await repository.GetByIdAsync(
                id: OrderId.From(value: @event.OrderId),
                cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException(
                message: $"Payment {@event.PaymentId} succeeded for order {@event.OrderId}, which no longer exists.");

        Result paid = order.MarkPaid();

        if (paid.IsError)
        {
            throw new InvalidOperationException(
                message: $"Marking order {@event.OrderId} paid after payment {@event.PaymentId} succeeded failed: {paid.FirstError.Code} — {paid.FirstError.Description}.");
        }

        repository.Update(aggregate: order);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
    }
}
