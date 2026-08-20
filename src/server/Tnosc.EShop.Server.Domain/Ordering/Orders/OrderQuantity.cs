// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Globalization;
using Tnosc.Lib.Shared.Results;
using Tnosc.Lib.Domain.ValueObjects;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// The number of units on a single order line.
/// </summary>
/// <remarks>
/// Deliberately Ordering's own type rather than a reuse of Basket's <c>Quantity</c>: the two contexts
/// must not reference each other, and their bounds are free to diverge — a basket line's ceiling is a
/// storefront nicety, an order line's is a commitment the warehouse has to honour.
/// </remarks>
public sealed record OrderQuantity : ValueObject
{
    /// <summary>
    /// The smallest quantity an order line may carry.
    /// </summary>
    public const int MinValue = 1;

    /// <summary>
    /// The largest quantity a single order line may carry.
    /// </summary>
    public const int MaxValue = 1000;

    private OrderQuantity(int value) => Value = value;

    /// <summary>
    /// Gets the number of units.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Creates an <see cref="OrderQuantity"/>, rejecting a value outside
    /// <see cref="MinValue"/>..<see cref="MaxValue"/>.
    /// </summary>
    /// <param name="value">The number of units.</param>
    /// <returns>The created <see cref="OrderQuantity"/>, or an <c>OrderQuantity.OutOfRange</c> validation error.</returns>
    public static Result<OrderQuantity> Create(int value)
    {
        if (value < MinValue || value > MaxValue)
        {
            return OrderQuantityErrors.OutOfRange;
        }

        return new OrderQuantity(value: value);
    }

    /// <summary>
    /// Returns the quantity in a human-readable form.
    /// </summary>
    /// <returns>The value of <see cref="Value"/>.</returns>
    public override string ToString() =>
        Value.ToString(provider: CultureInfo.InvariantCulture);
}
