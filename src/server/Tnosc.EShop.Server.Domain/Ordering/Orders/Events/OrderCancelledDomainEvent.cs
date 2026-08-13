// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders.Events;

/// <summary>
/// Raised when an order is cancelled before despatch.
/// </summary>
/// <remarks>
/// Carries the lines for the same reason <see cref="OrderPlacedDomainEvent"/> does: whatever the
/// placement event caused a subscriber to do, the cancellation is what lets it be undone from the
/// message alone. Catalog restocking is the obvious case, and it is a T14 concern rather than a T13
/// one — the payload is shaped for it now so the contract does not have to be versioned later.
/// </remarks>
/// <param name="Id">The domain event identifier.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="OrderId">The identifier of the cancelled order.</param>
/// <param name="OrderNumber">The order's human-facing reference.</param>
/// <param name="CustomerId">The identifier of the customer who placed the order.</param>
/// <param name="Lines">The order's lines.</param>
[DomainEventName("ordering.order-cancelled.v1")]
public sealed record OrderCancelledDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    OrderPlacedLine[] Lines) : IDomainEvent;
