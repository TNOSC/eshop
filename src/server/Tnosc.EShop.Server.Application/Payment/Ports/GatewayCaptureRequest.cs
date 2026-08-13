// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Payment.Ports;

/// <summary>
/// A request to capture funds through <see cref="IPaymentGateway"/>.
/// </summary>
/// <param name="PaymentId">The identifier of the payment being captured.</param>
/// <param name="OrderId">The identifier of the order the payment is for.</param>
/// <param name="Amount">The amount to capture.</param>
/// <param name="Currency">The three-letter ISO 4217 currency of the amount.</param>
/// <param name="GatewayReference">
/// The prior authorization's gateway reference, when capturing a previously authorized card
/// payment. <see langword="null"/> for a method that captures with no prior authorization.
/// </param>
/// <param name="PaymentReference">
/// The caller-supplied reference identifying the funding source, for a method that captures
/// directly with no prior authorization (a wallet id, for example).
/// </param>
public sealed record GatewayCaptureRequest(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string? GatewayReference,
    string? PaymentReference);
