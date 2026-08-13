// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Payment.Payments.Strategies;

/// <summary>
/// One way of settling a payment. Card requires authorize-then-capture, a wallet captures
/// immediately, cash on delivery skips authorization entirely and settles on delivery.
/// </summary>
/// <remarks>
/// The strategy pattern exists here so that "how does this method settle" never becomes a
/// <c>switch</c> in a handler. <see cref="PaymentMethodStrategyFactory"/> owns the selection; each
/// implementation only knows its own settlement rule.
/// </remarks>
public interface IPaymentMethodStrategy
{
    /// <summary>
    /// Gets a value indicating whether this method requires the gateway to authorize before a
    /// separate capture completes the payment.
    /// </summary>
    bool RequiresAuthorization { get; }

    /// <summary>
    /// Plans how a payment of <paramref name="amount"/> should be settled under this method.
    /// </summary>
    /// <param name="amount">The amount to plan for.</param>
    /// <returns>
    /// The settlement plan, or a validation/conflict error when this method refuses the amount (for
    /// example, cash on delivery over its limit).
    /// </returns>
    Result<PaymentPlan> Plan(Money amount);
}
