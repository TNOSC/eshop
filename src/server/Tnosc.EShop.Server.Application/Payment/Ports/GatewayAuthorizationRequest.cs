// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Payment.Ports;

/// <summary>
/// A request to authorize (reserve) funds through <see cref="IPaymentGateway"/>.
/// </summary>
/// <param name="PaymentId">The identifier of the payment being authorized.</param>
/// <param name="OrderId">The identifier of the order the payment is for.</param>
/// <param name="Amount">The amount to authorize.</param>
/// <param name="Currency">The three-letter ISO 4217 currency of the amount.</param>
/// <param name="PaymentReference">
/// The caller-supplied reference identifying the funding source — a card number for
/// <see cref="Tnosc.EShop.Server.Domain.Payment.Payments.PaymentMethod.Card"/>. Drives
/// <c>FakePaymentGateway</c>'s deterministic outcomes.
/// </param>
public sealed record GatewayAuthorizationRequest(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string? PaymentReference);
