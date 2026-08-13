// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Payment.Commands.CapturePayment;

/// <summary>
/// Captures a payment — completing a card's authorize-then-capture flow, or settling a cash-on-delivery
/// payment at delivery time.
/// </summary>
/// <param name="PaymentId">The identifier of the payment to capture.</param>
public sealed record CapturePaymentCommand(Guid PaymentId) : ICommand;
