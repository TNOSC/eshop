// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Globalization;
using Tnosc.Lib.Shared.Results;
using Tnosc.Lib.Domain.ValueObjects;

namespace Tnosc.EShop.Server.Domain.Catalog.Products;

/// <summary>
/// The number of units of a product currently on hand. Never negative.
/// </summary>
public sealed record StockQuantity : ValueObject
{
    private StockQuantity(int value) => Value = value;

    /// <summary>
    /// Gets the number of units on hand.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Creates a <see cref="StockQuantity"/>, rejecting negative values.
    /// </summary>
    /// <param name="value">The number of units on hand.</param>
    /// <returns>The created <see cref="StockQuantity"/>, or a <c>StockQuantity.Negative</c> validation error.</returns>
    public static Result<StockQuantity> Create(int value)
    {
        if (value < 0)
        {
            return StockQuantityErrors.Negative;
        }

        return new StockQuantity(value: value);
    }

    /// <summary>
    /// Applies a signed delta to this quantity, rejecting a result that would fall below zero.
    /// </summary>
    /// <param name="delta">The signed number of units to add or remove.</param>
    /// <returns>The adjusted <see cref="StockQuantity"/>, or a <c>StockQuantity.Negative</c> validation error.</returns>
    public Result<StockQuantity> Adjust(int delta) =>
        Create(value: Value + delta);

    /// <summary>
    /// Returns the quantity in a human-readable form.
    /// </summary>
    /// <returns>The value of <see cref="Value"/>.</returns>
    public override string ToString() =>
        Value.ToString(provider: CultureInfo.InvariantCulture);
}
