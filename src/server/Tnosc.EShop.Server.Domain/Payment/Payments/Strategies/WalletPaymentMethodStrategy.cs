// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Payment.Payments.Strategies;

/// <summary>
/// A wallet payment: the balance is debited immediately, with no separate authorization step.
/// </summary>
public sealed class WalletPaymentMethodStrategy : IPaymentMethodStrategy
{
    /// <inheritdoc />
    public bool RequiresAuthorization => false;

    /// <inheritdoc />
    public Result<PaymentPlan> Plan(Money amount)
    {
        ArgumentNullException.ThrowIfNull(argument: amount);

        if (amount.Amount <= 0)
        {
            return PaymentErrors.AmountMustBePositive;
        }

        return new PaymentPlan(Amount: amount, RequiresAuthorization: false, CapturesImmediately: true);
    }
}
