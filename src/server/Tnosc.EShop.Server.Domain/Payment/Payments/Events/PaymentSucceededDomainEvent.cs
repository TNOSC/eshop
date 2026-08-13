// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Payment.Payments.Events;

/// <summary>
/// Raised when a <see cref="Payment"/> is captured — the funds have actually been taken.
/// </summary>
/// <remarks>
/// The cross-context half of the loop: Ordering's <c>PaymentSucceededMarkOrderPaidHandler</c> reacts
/// to this and calls <c>Order.MarkPaid()</c>. The payload carries flat primitives only, per
/// <c>.claude/rules/domain-events.md</c> — <see cref="OrderId"/> is the plain <see cref="Guid"/>
/// Ordering itself stores it as, never a typed <c>OrderId</c> or <c>PaymentId</c>.
/// </remarks>
/// <param name="Id">The domain event identifier — the key the inbox dedupes redeliveries on.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="PaymentId">The identifier of the captured payment.</param>
/// <param name="OrderId">The identifier of the order the payment was for.</param>
/// <param name="AmountAmount">The captured amount.</param>
/// <param name="AmountCurrency">The three-letter ISO 4217 currency of the amount.</param>
[DomainEventName("payment.payment-succeeded.v1")]
public sealed record PaymentSucceededDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid PaymentId,
    Guid OrderId,
    decimal AmountAmount,
    string AmountCurrency) : IDomainEvent;
