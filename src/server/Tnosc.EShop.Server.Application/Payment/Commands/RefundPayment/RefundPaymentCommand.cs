// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Payment.Commands.RefundPayment;

/// <summary>
/// Refunds a previously captured payment.
/// </summary>
/// <param name="PaymentId">The identifier of the payment to refund.</param>
/// <param name="Reason">Why the payment is being refunded, when supplied.</param>
public sealed record RefundPaymentCommand(Guid PaymentId, string? Reason) : ICommand;
