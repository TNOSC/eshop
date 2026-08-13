// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders.Events;

/// <summary>
/// Raised when a customer places an order.
/// </summary>
/// <remarks>
/// <para>
/// The one genuinely cross-context event in this solution. It is written to the outbox in the same
/// transaction as the order itself, and the processor delivers it to two subscribers that live in
/// other bounded contexts and know nothing about Ordering's aggregate: Basket clears the customer's
/// basket, Catalog decrements stock for each line. That is why the payload carries
/// <see cref="CustomerId"/> and the full <see cref="Lines"/> set — each subscriber must be able to do
/// its work from the message alone, without reaching back into Ordering.
/// </para>
/// <para>
/// Delivery is at-least-once, so both subscribers must tolerate seeing this twice. See
/// <c>.claude/rules/domain-events.md</c>.
/// </para>
/// </remarks>
/// <param name="Id">The domain event identifier — the key the inbox dedupes redeliveries on.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="OrderId">The identifier of the placed order.</param>
/// <param name="OrderNumber">The order's human-facing reference.</param>
/// <param name="CustomerId">The identifier of the customer who placed the order.</param>
/// <param name="TotalAmount">The order's total after any discount.</param>
/// <param name="TotalCurrency">The three-letter ISO 4217 currency of the total.</param>
/// <param name="Lines">The order's lines.</param>
[DomainEventName("ordering.order-placed.v1")]
public sealed record OrderPlacedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    decimal TotalAmount,
    string TotalCurrency,
    OrderPlacedLine[] Lines) : IDomainEvent;
