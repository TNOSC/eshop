// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Payment.Commands.InitiatePayment;

namespace Tnosc.EShop.Server.Api.Payment.InitiatePayment;

/// <summary>
/// The HTTP body of <c>POST /api/payments</c>.
/// </summary>
/// <param name="OrderId">The identifier of the order to pay for.</param>
/// <param name="AmountAmount">The amount to collect.</param>
/// <param name="AmountCurrency">The three-letter ISO 4217 currency of the amount.</param>
/// <param name="Method">How the customer is paying — <c>Card</c>, <c>Wallet</c> or <c>CashOnDelivery</c>.</param>
/// <param name="PaymentReference">
/// The funding-source reference — a card number for <c>Card</c>, a wallet id for <c>Wallet</c>,
/// omitted for <c>CashOnDelivery</c>.
/// </param>
internal sealed record InitiatePaymentRequest(
    Guid OrderId,
    decimal AmountAmount,
    string? AmountCurrency,
    string? Method,
    string? PaymentReference)
{
    /// <summary>
    /// Maps this request onto the application command it carries.
    /// </summary>
    /// <returns>The equivalent <see cref="InitiatePaymentCommand"/>.</returns>
    public InitiatePaymentCommand ToCommand() =>
        new(OrderId: OrderId,
            AmountAmount: AmountAmount,
            AmountCurrency: AmountCurrency ?? string.Empty,
            Method: Method ?? string.Empty,
            PaymentReference: PaymentReference);
}
