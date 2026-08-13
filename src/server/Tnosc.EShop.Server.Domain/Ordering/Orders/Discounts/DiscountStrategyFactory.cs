// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Domain.Shared;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders.Discounts;

/// <summary>
/// Chooses which <see cref="IDiscountStrategy"/> an order qualifies for.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This is the only place the selection rule exists.</strong> The <c>switch</c> on tier and
/// subtotal lives here, in the domain — not in <c>OrderInitializer</c>, not in
/// <c>PlaceOrderWorkflow</c>, and not in <c>PlaceOrderCommandHandler</c>, all of which would be
/// rejected by <c>NoBusinessBranchingTests</c> and, more importantly, would put a pricing decision
/// where nobody looks for one.
/// </para>
/// <para>
/// The pairing to keep in mind: the factory answers "which scheme", each strategy answers "how much
/// under that scheme". Changing who qualifies is a change here; changing what a scheme pays is a
/// change in the strategy.
/// </para>
/// </remarks>
public static class DiscountStrategyFactory
{
    /// <summary>
    /// The subtotal at or above which even a <see cref="CustomerTier.Standard"/> customer earns a
    /// discount.
    /// </summary>
    public const decimal StandardTierThreshold = 250m;

    /// <summary>
    /// The fraction a <see cref="CustomerTier.Standard"/> customer earns past
    /// <see cref="StandardTierThreshold"/>.
    /// </summary>
    public const decimal StandardTierPercentage = 0.02m;

    /// <summary>
    /// The fraction a <see cref="CustomerTier.Silver"/> customer earns on any subtotal.
    /// </summary>
    public const decimal SilverTierPercentage = 0.05m;

    /// <summary>
    /// Returns the discount strategy an order of this size, placed by a customer of this tier,
    /// qualifies for.
    /// </summary>
    /// <param name="total">The order's undiscounted subtotal.</param>
    /// <param name="tier">The placing customer's tier.</param>
    /// <returns>
    /// Never <see langword="null"/> — a customer who qualifies for nothing gets
    /// <see cref="NoDiscountStrategy"/>, so the caller has no absent case to branch on.
    /// </returns>
    public static IDiscountStrategy Create(Money total, CustomerTier tier)
    {
        ArgumentNullException.ThrowIfNull(argument: total);

        return tier switch
        {
            CustomerTier.Gold => new TieredDiscountStrategy(),
            CustomerTier.Silver => new PercentageDiscountStrategy(percentage: SilverTierPercentage),
            _ when total.Amount >= StandardTierThreshold => new PercentageDiscountStrategy(percentage: StandardTierPercentage),
            _ => new NoDiscountStrategy(),
        };
    }
}
