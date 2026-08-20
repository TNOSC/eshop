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
/// Cash paid on delivery: no gateway is contacted at initiation, and the payment settles only when
/// captured at delivery time.
/// </summary>
public sealed class CashOnDeliveryPaymentMethodStrategy : IPaymentMethodStrategy
{
    /// <summary>
    /// The largest amount cash on delivery covers. A courier collecting cash does not carry unlimited
    /// change, so the scheme caps what it will settle.
    /// </summary>
    public const decimal Limit = 1000m;

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

        if (amount.Amount > Limit)
        {
            return PaymentErrors.CashOnDeliveryLimitExceeded(limit: Limit);
        }

        return new PaymentPlan(Amount: amount, RequiresAuthorization: false, CapturesImmediately: false);
    }
}
