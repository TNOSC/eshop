// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Globalization;
using Tnosc.Lib.Domain.Results;
using Tnosc.Lib.Domain.ValueObjects;

namespace Tnosc.EShop.Server.Domain.Basket.Baskets;

/// <summary>
/// The number of units of a single basket line. Bounded so a runaway client cannot push a single
/// line to an unreasonable quantity.
/// </summary>
public sealed record Quantity : ValueObject
{
    /// <summary>
    /// The smallest quantity a basket line may carry.
    /// </summary>
    public const int MinValue = 1;

    /// <summary>
    /// The largest quantity a single basket line may carry.
    /// </summary>
    public const int MaxValue = 100;

    private Quantity(int value) => Value = value;

    /// <summary>
    /// Gets the number of units.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Creates a <see cref="Quantity"/>, rejecting a value outside <see cref="MinValue"/>..<see cref="MaxValue"/>.
    /// </summary>
    /// <param name="value">The number of units.</param>
    /// <returns>The created <see cref="Quantity"/>, or a <c>Quantity.OutOfRange</c> validation error.</returns>
    public static Result<Quantity> Create(int value)
    {
        if (value < MinValue || value > MaxValue)
        {
            return QuantityErrors.OutOfRange;
        }

        return new Quantity(value: value);
    }

    /// <summary>
    /// Adds another <see cref="Quantity"/> to this one, rejecting a sum outside the allowed range.
    /// </summary>
    /// <param name="other">The quantity to add.</param>
    /// <returns>The combined <see cref="Quantity"/>, or a <c>Quantity.OutOfRange</c> validation error.</returns>
    public Result<Quantity> Add(Quantity other) =>
        Create(value: Value + other.Value);

    /// <summary>
    /// Returns the quantity in a human-readable form.
    /// </summary>
    /// <returns>The value of <see cref="Value"/>.</returns>
    public override string ToString() =>
        Value.ToString(provider: CultureInfo.InvariantCulture);
}
