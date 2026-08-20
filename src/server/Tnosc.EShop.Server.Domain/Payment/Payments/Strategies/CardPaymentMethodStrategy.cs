// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Domain.Payment.Payments.Strategies;

/// <summary>
/// A card payment: the gateway authorizes a hold on the funds first, and a separate capture step
/// completes the payment.
/// </summary>
public sealed class CardPaymentMethodStrategy : IPaymentMethodStrategy
{
    /// <inheritdoc />
    public bool RequiresAuthorization => true;

    /// <inheritdoc />
    public Result<PaymentPlan> Plan(Money amount)
    {
        ArgumentNullException.ThrowIfNull(argument: amount);

        if (amount.Amount <= 0)
        {
            return PaymentErrors.AmountMustBePositive;
        }

        return new PaymentPlan(Amount: amount, RequiresAuthorization: true, CapturesImmediately: false);
    }
}
