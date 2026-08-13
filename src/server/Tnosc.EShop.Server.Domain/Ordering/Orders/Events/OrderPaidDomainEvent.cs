// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders.Events;

/// <summary>
/// Raised when an order's payment settles.
/// </summary>
/// <param name="Id">The domain event identifier.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="OrderId">The identifier of the paid order.</param>
/// <param name="OrderNumber">The order's human-facing reference.</param>
/// <param name="CustomerId">The identifier of the customer who placed the order.</param>
/// <param name="TotalAmount">The amount paid.</param>
/// <param name="TotalCurrency">The three-letter ISO 4217 currency of the amount paid.</param>
[DomainEventName("ordering.order-paid.v1")]
public sealed record OrderPaidDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    decimal TotalAmount,
    string TotalCurrency) : IDomainEvent;
