// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.EShop.Server.Domain.Shared;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders.Discounts;

/// <summary>
/// One way of discounting an order's subtotal.
/// </summary>
/// <remarks>
/// The strategy pattern exists here so that "how much comes off" never becomes a <c>switch</c> in a
/// handler. Each implementation knows one pricing rule and nothing about when it applies;
/// <see cref="DiscountStrategyFactory"/> owns the selection. Adding a rule is a new implementation
/// plus one arm in the factory — no handler, workflow or endpoint changes.
/// </remarks>
public interface IDiscountStrategy
{
    /// <summary>
    /// Gets a short, stable name for the rule this strategy applies, for logging and tests.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Applies this strategy's discount to an order subtotal.
    /// </summary>
    /// <param name="total">The order's undiscounted subtotal.</param>
    /// <returns>The discounted total. Never greater than <paramref name="total"/>, never negative.</returns>
    Money Apply(Money total);
}
