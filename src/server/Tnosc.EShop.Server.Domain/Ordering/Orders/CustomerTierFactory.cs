// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// Maps a customer's order history onto the <see cref="CustomerTier"/> it earns them.
/// </summary>
/// <remarks>
/// A factory rather than a method on a step service for the same reason
/// <see cref="DiscountStrategyFactory"/> is one: "how many orders makes someone Gold" is a business
/// rule, and a <c>switch</c> on it inside a workflow step would be exactly the branching the
/// architecture rules push into the domain. The step asks for the count and hands it here.
/// </remarks>
public static class CustomerTierFactory
{
    /// <summary>
    /// The number of previous orders at which a customer becomes <see cref="CustomerTier.Silver"/>.
    /// </summary>
    public const int SilverThreshold = 3;

    /// <summary>
    /// The number of previous orders at which a customer becomes <see cref="CustomerTier.Gold"/>.
    /// </summary>
    public const int GoldThreshold = 10;

    /// <summary>
    /// Returns the tier the supplied order history earns.
    /// </summary>
    /// <param name="previousOrderCount">How many orders the customer has already placed.</param>
    /// <returns>The earned <see cref="CustomerTier"/>.</returns>
    public static CustomerTier For(int previousOrderCount) => previousOrderCount switch
    {
        >= GoldThreshold => CustomerTier.Gold,
        >= SilverThreshold => CustomerTier.Silver,
        _ => CustomerTier.Standard,
    };
}
