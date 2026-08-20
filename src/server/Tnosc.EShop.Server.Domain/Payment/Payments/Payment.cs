// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Domain.Payment.Payments.Events;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Domain.Payment.Payments;

/// <summary>
/// An attempt to collect money for an order. Owns its own lifecycle: which transition is legal from
/// which status is decided here and nowhere else.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The status machine belongs to this class.</strong> No handler or endpoint ever compares a
/// <see cref="PaymentStatus"/> — each of <see cref="Authorize"/>, <see cref="Capture"/>,
/// <see cref="Fail"/> and <see cref="Refund"/> decides for itself whether it is reachable from the
/// current status and hands back a <c>Conflict</c> when it is not.
/// </para>
/// <para>
/// <see cref="OrderId"/> is a plain <see cref="Guid"/>, never an <c>OrderId</c> — Payment must not
/// reference Ordering's aggregate, only the plain identifier Ordering itself stores.
/// </para>
/// </remarks>
public sealed class Payment : AggregateRoot<PaymentId>
{
    private Payment()
    {
        // EF.
    }

    /// <summary>
    /// Gets the identifier of the order this payment is for.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// Gets the amount this payment covers.
    /// </summary>
    public Money Amount { get; private set; } = null!;

    /// <summary>
    /// Gets how the customer is paying.
    /// </summary>
    public PaymentMethod Method { get; private set; }

    /// <summary>
    /// Gets where the payment stands in its lifecycle.
    /// </summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>
    /// Gets the gateway's reference for this payment — an authorization id once
    /// <see cref="Authorize"/> has run, a capture id once <see cref="Capture"/> has, or
    /// <see langword="null"/> until either has.
    /// </summary>
    public string? GatewayReference { get; private set; }

    /// <summary>
    /// Gets why the payment failed, once <see cref="Fail"/> has run.
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// Creates a pending payment for an order.
    /// </summary>
    /// <remarks>
    /// Deliberately <see langword="internal"/>: "one payment per order" spans the whole payment
    /// ledger, so it cannot be decided here. <see cref="PaymentFactory"/> is the only reachable way
    /// in from outside the domain.
    /// </remarks>
    /// <param name="orderId">The identifier of the order this payment is for.</param>
    /// <param name="amount">The amount to collect.</param>
    /// <param name="method">How the customer is paying.</param>
    /// <returns>The created, <see cref="PaymentStatus.Pending"/> payment.</returns>
    internal static Payment Create(Guid orderId, Money amount, PaymentMethod method)
    {
        ArgumentNullException.ThrowIfNull(argument: amount);

        var payment = new Payment
        {
            Id = PaymentId.New(),
            OrderId = orderId,
            Amount = amount,
            Method = method,
            Status = PaymentStatus.Pending,
        };

        payment.IncrementVersion();

        return payment;
    }

    /// <summary>
    /// Records that the gateway has authorized (reserved) the funds.
    /// </summary>
    /// <param name="gatewayReference">The gateway's reference for the authorization.</param>
    /// <returns>Success, or a <c>Payment.CannotAuthorize</c> conflict when the payment is not pending.</returns>
    public Result Authorize(string gatewayReference)
    {
        if (Status != PaymentStatus.Pending)
        {
            return PaymentErrors.CannotAuthorize(status: Status);
        }

        Status = PaymentStatus.Authorized;
        GatewayReference = gatewayReference;
        IncrementVersion();

        return Result.Success();
    }

    /// <summary>
    /// Records that the funds have been taken and raises <see cref="PaymentSucceededDomainEvent"/>.
    /// </summary>
    /// <remarks>
    /// Legal from <see cref="PaymentStatus.Pending"/> (a wallet capturing immediately, or cash on
    /// delivery settling at delivery time) or from <see cref="PaymentStatus.Authorized"/> (a card
    /// completing its two-step flow).
    /// </remarks>
    /// <param name="gatewayReference">The gateway's reference for the capture.</param>
    /// <returns>Success, or a <c>Payment.CannotCapture</c> conflict when the payment cannot be captured.</returns>
    public Result Capture(string gatewayReference)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Authorized))
        {
            return PaymentErrors.CannotCapture(status: Status);
        }

        Status = PaymentStatus.Captured;
        GatewayReference = gatewayReference;
        IncrementVersion();

        AddDomainEvent(domainEvent: new PaymentSucceededDomainEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: DateTime.UtcNow,
            PaymentId: Id.Value,
            OrderId: OrderId,
            AmountAmount: Amount.Amount,
            AmountCurrency: Amount.Currency));

        return Result.Success();
    }

    /// <summary>
    /// Records that the payment could not go through and raises <see cref="PaymentFailedDomainEvent"/>.
    /// </summary>
    /// <remarks>
    /// A declined card is a business outcome the gateway reported as data — this is the domain
    /// deciding what that data means, not a technical failure.
    /// </remarks>
    /// <param name="reason">Why the payment failed.</param>
    /// <returns>Success, or a <c>Payment.CannotFail</c> conflict when the payment is already settled.</returns>
    public Result Fail(string reason)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Authorized))
        {
            return PaymentErrors.CannotFail(status: Status);
        }

        Status = PaymentStatus.Failed;
        FailureReason = reason;
        IncrementVersion();

        AddDomainEvent(domainEvent: new PaymentFailedDomainEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: DateTime.UtcNow,
            PaymentId: Id.Value,
            OrderId: OrderId,
            Reason: reason));

        return Result.Success();
    }

    /// <summary>
    /// Returns a previously captured payment to the customer and raises
    /// <see cref="PaymentRefundedDomainEvent"/>.
    /// </summary>
    /// <param name="reason">Why the payment is being refunded, when supplied.</param>
    /// <returns>Success, or a <c>Payment.CannotRefund</c> conflict when the payment was not captured.</returns>
    public Result Refund(string? reason)
    {
        if (Status != PaymentStatus.Captured)
        {
            return PaymentErrors.CannotRefund(status: Status);
        }

        Status = PaymentStatus.Refunded;
        IncrementVersion();

        AddDomainEvent(domainEvent: new PaymentRefundedDomainEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: DateTime.UtcNow,
            PaymentId: Id.Value,
            OrderId: OrderId,
            AmountAmount: Amount.Amount,
            AmountCurrency: Amount.Currency,
            Reason: reason));

        return Result.Success();
    }
}
