// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Payment.Queries.GetPaymentByOrder;

/// <summary>
/// A payment, as the query side returns it.
/// </summary>
/// <param name="Id">The payment's identifier.</param>
/// <param name="OrderId">The identifier of the order the payment is for.</param>
/// <param name="AmountAmount">The amount the payment covers.</param>
/// <param name="AmountCurrency">The three-letter ISO 4217 currency of the amount.</param>
/// <param name="Method">How the customer paid, as its name.</param>
/// <param name="Status">The payment's status, as its name.</param>
/// <param name="GatewayReference">The gateway's reference, once authorized or captured.</param>
/// <param name="FailureReason">Why the payment failed, when it did.</param>
public sealed record PaymentDto(
    Guid Id,
    Guid OrderId,
    decimal AmountAmount,
    string AmountCurrency,
    string Method,
    string Status,
    string? GatewayReference,
    string? FailureReason);
