// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Payment.Commands.InitiatePayment;

/// <summary>
/// Initiates a payment for an order.
/// </summary>
/// <param name="OrderId">The identifier of the order to pay for.</param>
/// <param name="AmountAmount">The amount to collect.</param>
/// <param name="AmountCurrency">The three-letter ISO 4217 currency of the amount.</param>
/// <param name="Method">How the customer is paying, as its name.</param>
/// <param name="PaymentReference">
/// The caller-supplied reference identifying the funding source — a card number for
/// <see cref="PaymentMethod.Card"/>, a wallet id for <see cref="PaymentMethod.Wallet"/>, unused for
/// <see cref="PaymentMethod.CashOnDelivery"/>.
/// </param>
public sealed record InitiatePaymentCommand(
    Guid OrderId,
    decimal AmountAmount,
    string AmountCurrency,
    string Method,
    string? PaymentReference) : ICommand<PaymentId>;
