// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Domain.Payment.Payments;

/// <summary>
/// How a customer pays for an order. Selects which <see cref="Strategies.IPaymentMethodStrategy"/>
/// <see cref="Strategies.PaymentMethodStrategyFactory"/> hands back.
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// A card payment. Requires the gateway to authorize before a separate capture completes it.
    /// </summary>
    Card = 0,

    /// <summary>
    /// A wallet payment (for example a stored balance). Captures immediately — no separate
    /// authorization step.
    /// </summary>
    Wallet = 1,

    /// <summary>
    /// Cash paid on delivery. Skips authorization entirely; the payment settles when captured at
    /// delivery time.
    /// </summary>
    CashOnDelivery = 2,
}
