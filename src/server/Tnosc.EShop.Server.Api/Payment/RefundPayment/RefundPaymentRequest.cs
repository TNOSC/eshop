// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Payment.Commands.RefundPayment;

namespace Tnosc.EShop.Server.Api.Payment.RefundPayment;

/// <summary>
/// The HTTP body of <c>POST /api/payments/{id}/refund</c>.
/// </summary>
/// <param name="Reason">Why the payment is being refunded, when supplied.</param>
internal sealed record RefundPaymentRequest(string? Reason)
{
    /// <summary>
    /// Maps this request onto the application command it carries.
    /// </summary>
    /// <param name="paymentId">The identifier of the payment to refund, taken from the route.</param>
    /// <returns>The equivalent <see cref="RefundPaymentCommand"/>.</returns>
    public RefundPaymentCommand ToCommand(Guid paymentId) =>
        new(PaymentId: paymentId, Reason: Reason);
}
