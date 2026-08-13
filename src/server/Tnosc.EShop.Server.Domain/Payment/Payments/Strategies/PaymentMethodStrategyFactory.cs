// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Domain.Payment.Payments.Strategies;

/// <summary>
/// Chooses which <see cref="IPaymentMethodStrategy"/> a <see cref="PaymentMethod"/> settles under.
/// </summary>
/// <remarks>
/// <strong>This is the only place the selection lives.</strong> Not in
/// <c>InitiatePaymentCommandHandler</c>, not in any endpoint — both would be rejected by
/// <c>NoBusinessBranchingTests</c>, and more importantly would scatter "how does this method settle"
/// across places nobody looks for it.
/// </remarks>
public static class PaymentMethodStrategyFactory
{
    /// <summary>
    /// Returns the strategy a payment method settles under.
    /// </summary>
    /// <param name="method">The payment method to select a strategy for.</param>
    /// <returns>Never <see langword="null"/> — every defined <see cref="PaymentMethod"/> has one.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="method"/> is not one of the defined <see cref="PaymentMethod"/> values.
    /// </exception>
    public static IPaymentMethodStrategy Create(PaymentMethod method) =>
        method switch
        {
            PaymentMethod.Card => new CardPaymentMethodStrategy(),
            PaymentMethod.Wallet => new WalletPaymentMethodStrategy(),
            PaymentMethod.CashOnDelivery => new CashOnDeliveryPaymentMethodStrategy(),
            _ => throw new ArgumentOutOfRangeException(paramName: nameof(method), actualValue: method, message: "Unknown payment method."),
        };
}
