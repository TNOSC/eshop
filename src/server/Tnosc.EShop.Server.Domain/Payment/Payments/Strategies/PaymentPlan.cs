// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.EShop.Server.Domain.Shared;

namespace Tnosc.EShop.Server.Domain.Payment.Payments.Strategies;

/// <summary>
/// How a payment method wants a given amount handled, as decided by an
/// <see cref="IPaymentMethodStrategy"/>.
/// </summary>
/// <param name="Amount">The amount the plan covers.</param>
/// <param name="RequiresAuthorization">
/// Whether the gateway must authorize before a separate capture can complete the payment — true for
/// <see cref="PaymentMethod.Card"/>, false for every other method.
/// </param>
/// <param name="CapturesImmediately">
/// Whether the payment should be captured in the same step it is initiated, with no authorization in
/// between — true for <see cref="PaymentMethod.Wallet"/>. False for <see cref="PaymentMethod.Card"/>
/// (capture follows authorization as a separate step) and for
/// <see cref="PaymentMethod.CashOnDelivery"/> (capture happens later, on delivery).
/// </param>
public sealed record PaymentPlan(Money Amount, bool RequiresAuthorization, bool CapturesImmediately);
