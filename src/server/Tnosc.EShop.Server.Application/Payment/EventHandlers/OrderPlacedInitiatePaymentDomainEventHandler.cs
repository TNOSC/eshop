// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Payment.Commands.InitiatePayment;
using Tnosc.EShop.Server.Domain.Ordering.Orders.Events;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Payment.EventHandlers;

/// <summary>
/// Opens a payment for an order once it has been placed.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Payment's handler, in Payment's folder, over Payment's own aggregate.</strong> It reads
/// Ordering's published event and never touches an <c>Order</c>; Ordering, for its part, never writes
/// to a <c>Payment</c>. That is the whole of the coupling between the two contexts.
/// </para>
/// <para>
/// <strong>Judgment call: this reaction settles by <see cref="PaymentMethod.Wallet"/>.</strong>
/// <c>OrderPlacedDomainEvent</c> carries no chosen payment method — Ordering does not model checkout
/// payment selection in this slice — so the automatic reaction cannot know whether the customer meant
/// to pay by card. A wallet payment captures immediately with no card details required, which is what
/// lets an order become <c>Paid</c> purely through the outbox with no further client interaction, and
/// is the safest default to pick automatically on someone else's behalf. Paying by card is still
/// fully supported — a caller drives it explicitly through <c>POST /api/payments</c>, which is also
/// how this slice's declining-card walkthrough is exercised. A future slice that lets Ordering carry
/// the customer's chosen method at checkout would replace this default; it is out of scope here.
/// </para>
/// <para>
/// <strong><c>[Idempotent]</c> is load-bearing here.</strong> Outbox delivery is at-least-once, and
/// initiating a payment is exactly the non-idempotent effect the attribute exists for: a redelivery
/// without it would try to open a second payment for the same order. The decorator claims
/// <c>OrderPlacedDomainEvent.Id</c> for this handler in the same transaction as the command's own
/// writes, so a replay is skipped rather than re-attempted. <c>PaymentFactory</c>'s own
/// "one payment per order" uniqueness rule is the second line of defense for any legitimate race
/// (for example, a manual <c>POST /api/payments</c> call reaching an order first).
/// </para>
/// </remarks>
/// <param name="initiatePayment">The Payment command handler that owns opening a payment.</param>
[Idempotent]
internal sealed class OrderPlacedInitiatePaymentDomainEventHandler(ICommandHandler<InitiatePaymentCommand, PaymentId> initiatePayment)
    : IDomainEventHandler<OrderPlacedDomainEvent>
{
    /// <summary>
    /// The funding-source reference this automatic reaction settles wallet payments with. A fixed,
    /// always-approving placeholder — there is no real wallet balance behind it in this reference
    /// implementation.
    /// </summary>
    public const string AutomaticWalletReferencePrefix = "wallet-";

    /// <inheritdoc />
    public async ValueTask HandleAsync(OrderPlacedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: @event);

        Result<PaymentId> initiated = await initiatePayment.HandleAsync(
            command: new InitiatePaymentCommand(
                OrderId: @event.OrderId,
                AmountAmount: @event.TotalAmount,
                AmountCurrency: @event.TotalCurrency,
                Method: nameof(PaymentMethod.Wallet),
                PaymentReference: AutomaticWalletReferencePrefix + @event.CustomerId.ToString(format: "N", provider: CultureInfo.InvariantCulture)),
            cancellationToken: cancellationToken);

        // The command pipeline turns a failure into a Result rather than letting it escape, so a
        // silent `return` here would mark the outbox row processed for work that did not happen.
        // Throwing hands the row back to the processor, which retries it and eventually dead-letters it.
        if (initiated.IsError)
        {
            throw new InvalidOperationException(
                message: $"Initiating payment for order {@event.OrderNumber} failed: {initiated.FirstError.Code} — {initiated.FirstError.Description}.");
        }
    }
}
