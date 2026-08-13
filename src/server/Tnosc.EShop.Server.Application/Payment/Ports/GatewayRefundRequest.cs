// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Payment.Ports;

/// <summary>
/// A request to refund previously captured funds through <see cref="IPaymentGateway"/>.
/// </summary>
/// <param name="PaymentId">The identifier of the payment being refunded.</param>
/// <param name="OrderId">The identifier of the order the payment was for.</param>
/// <param name="Amount">The amount to refund.</param>
/// <param name="Currency">The three-letter ISO 4217 currency of the amount.</param>
/// <param name="GatewayReference">The original capture's gateway reference.</param>
public sealed record GatewayRefundRequest(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string? GatewayReference);
