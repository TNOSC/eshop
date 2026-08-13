// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders.Events;

/// <summary>
/// Raised when an order is confirmed and becomes payable.
/// </summary>
/// <param name="Id">The domain event identifier.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="OrderId">The identifier of the confirmed order.</param>
/// <param name="OrderNumber">The order's human-facing reference.</param>
/// <param name="CustomerId">The identifier of the customer who placed the order.</param>
/// <param name="TotalAmount">The order's total after any discount.</param>
/// <param name="TotalCurrency">The three-letter ISO 4217 currency of the total.</param>
[DomainEventName("ordering.order-confirmed.v1")]
public sealed record OrderConfirmedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    decimal TotalAmount,
    string TotalCurrency) : IDomainEvent;
