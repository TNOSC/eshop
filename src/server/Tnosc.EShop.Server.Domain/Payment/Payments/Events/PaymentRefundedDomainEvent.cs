// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Payment.Payments.Events;

/// <summary>
/// Raised when a captured <see cref="Payment"/> is refunded.
/// </summary>
/// <remarks>
/// No subscriber consumes this in the current slice — Ordering has no "refunded" status to react
/// with — but it is raised for the same reason every other state transition is: an audit trail in the
/// outbox and a seam a future context (notifications, accounting) can subscribe to without touching
/// this aggregate.
/// </remarks>
/// <param name="Id">The domain event identifier — the key the inbox dedupes redeliveries on.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="PaymentId">The identifier of the refunded payment.</param>
/// <param name="OrderId">The identifier of the order the payment was for.</param>
/// <param name="AmountAmount">The refunded amount.</param>
/// <param name="AmountCurrency">The three-letter ISO 4217 currency of the amount.</param>
/// <param name="Reason">Why the payment was refunded, when supplied.</param>
[DomainEventName("payment.payment-refunded.v1")]
public sealed record PaymentRefundedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid PaymentId,
    Guid OrderId,
    decimal AmountAmount,
    string AmountCurrency,
    string? Reason) : IDomainEvent;
