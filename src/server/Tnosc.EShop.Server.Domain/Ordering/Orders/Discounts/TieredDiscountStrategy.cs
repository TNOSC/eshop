// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Domain.Shared;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders.Discounts;

/// <summary>
/// Deducts a percentage that grows with the size of the basket.
/// </summary>
/// <remarks>
/// The band boundaries are this strategy's own rule, not the factory's. The factory decides
/// <em>that</em> a customer is on the tiered scheme; this decides what the scheme pays out at a given
/// subtotal, so widening the bands touches one file.
/// </remarks>
public sealed class TieredDiscountStrategy : IDiscountStrategy
{
    /// <summary>
    /// The subtotal at or above which the highest band applies.
    /// </summary>
    public const decimal UpperBandThreshold = 1000m;

    /// <summary>
    /// The subtotal at or above which the middle band applies.
    /// </summary>
    public const decimal MiddleBandThreshold = 500m;

    /// <summary>
    /// The fraction deducted at or above <see cref="UpperBandThreshold"/>.
    /// </summary>
    public const decimal UpperBandPercentage = 0.15m;

    /// <summary>
    /// The fraction deducted at or above <see cref="MiddleBandThreshold"/>.
    /// </summary>
    public const decimal MiddleBandPercentage = 0.10m;

    /// <summary>
    /// The fraction deducted below <see cref="MiddleBandThreshold"/>.
    /// </summary>
    public const decimal LowerBandPercentage = 0.05m;

    /// <inheritdoc />
    public string Name => "Tiered";

    /// <inheritdoc />
    public Money Apply(Money total)
    {
        ArgumentNullException.ThrowIfNull(argument: total);

        return total.Scale(factor: 1m - PercentageFor(amount: total.Amount));
    }

    /// <summary>
    /// Returns the fraction this strategy deducts at the supplied subtotal.
    /// </summary>
    /// <param name="amount">The order's undiscounted subtotal amount.</param>
    /// <returns>The fraction to deduct.</returns>
    public static decimal PercentageFor(decimal amount) => amount switch
    {
        >= UpperBandThreshold => UpperBandPercentage,
        >= MiddleBandThreshold => MiddleBandPercentage,
        _ => LowerBandPercentage,
    };
}
