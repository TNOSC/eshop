// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.EShop.Server.Domain.Shared;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders.Discounts;

/// <summary>
/// Charges the subtotal as it stands.
/// </summary>
/// <remarks>
/// A real strategy rather than a <see langword="null"/> the caller has to test for. Because this
/// exists, <see cref="DiscountStrategyFactory.Create"/> always returns something and
/// <c>Order.ApplyDiscount</c> never needs a "was there a discount?" branch — the null-object pattern
/// doing exactly the job the no-business-branching rule wants done.
/// </remarks>
public sealed class NoDiscountStrategy : IDiscountStrategy
{
    /// <inheritdoc />
    public string Name => "None";

    /// <inheritdoc />
    public Money Apply(Money total) => total;
}
