// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Payment.Payments.Events;

/// <summary>
/// Raised when a <see cref="Payment"/> is declined or otherwise cannot go through.
/// </summary>
/// <remarks>
/// Ordering's <c>PaymentFailedCancelOrderHandler</c> reacts to this and calls <c>Order.Cancel()</c>.
/// A declined card is a <em>business</em> outcome the gateway reported as data, not a technical
/// failure — this event is how that verdict crosses into Ordering.
/// </remarks>
/// <param name="Id">The domain event identifier — the key the inbox dedupes redeliveries on.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="PaymentId">The identifier of the failed payment.</param>
/// <param name="OrderId">The identifier of the order the payment was for.</param>
/// <param name="Reason">Why the payment failed, for display and support.</param>
[DomainEventName("payment.payment-failed.v1")]
public sealed record PaymentFailedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid PaymentId,
    Guid OrderId,
    string Reason) : IDomainEvent;
