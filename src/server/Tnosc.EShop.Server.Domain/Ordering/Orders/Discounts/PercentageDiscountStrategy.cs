// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Globalization;
using Tnosc.EShop.Server.Domain.Shared;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders.Discounts;

/// <summary>
/// Takes a flat percentage off the whole subtotal, whatever it is.
/// </summary>
/// <param name="percentage">The fraction to deduct, between <c>0m</c> and <c>1m</c> — <c>0.05m</c> for 5%.</param>
/// <exception cref="ArgumentOutOfRangeException"><paramref name="percentage"/> is outside 0..1.</exception>
public sealed class PercentageDiscountStrategy(decimal percentage) : IDiscountStrategy
{
    private readonly decimal _percentage = Validate(percentage: percentage);

    /// <inheritdoc />
    public string Name => string.Create(
        provider: CultureInfo.InvariantCulture,
        handler: $"Percentage({_percentage:P0})");

    /// <summary>
    /// Gets the fraction deducted from the subtotal.
    /// </summary>
    public decimal Percentage => _percentage;

    /// <inheritdoc />
    public Money Apply(Money total)
    {
        ArgumentNullException.ThrowIfNull(argument: total);

        return total.Scale(factor: 1m - _percentage);
    }

    private static decimal Validate(decimal percentage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value: percentage);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value: percentage, other: 1m);

        return percentage;
    }
}
