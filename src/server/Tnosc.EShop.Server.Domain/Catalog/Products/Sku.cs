// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain.Results;
using Tnosc.Lib.Domain.ValueObjects;

namespace Tnosc.EShop.Server.Domain.Catalog.Products;

/// <summary>
/// A product's stock-keeping unit: a short, human-readable, catalogue-unique code.
/// </summary>
/// <remarks>
/// Format is part of the invariant, uniqueness is not — uniqueness spans the whole catalogue and so
/// belongs to <see cref="ProductFactory"/>, which can consult <see cref="IProductRepository"/>.
/// </remarks>
public sealed record Sku : ValueObject
{
    /// <summary>
    /// The maximum number of characters a SKU may contain.
    /// </summary>
    public const int MaxLength = 32;

    private Sku(string value) => Value = value;

    /// <summary>
    /// Gets the normalized SKU text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a <see cref="Sku"/> from its text form, validating the format invariant.
    /// </summary>
    /// <param name="value">The candidate SKU text. Uppercase ASCII letters, digits and dashes only.</param>
    /// <returns>
    /// The created <see cref="Sku"/>, or <c>Sku.Empty</c> / <c>Sku.TooLong</c> / <c>Sku.InvalidFormat</c>
    /// validation errors when <paramref name="value"/> breaks the format invariant.
    /// </returns>
    public static Result<Sku> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value: value))
        {
            return SkuErrors.Empty;
        }

        if (value.Length > MaxLength)
        {
            return SkuErrors.TooLong;
        }

        if (!HasValidFormat(value: value))
        {
            return SkuErrors.InvalidFormat;
        }

        return new Sku(value: value);
    }

    /// <summary>
    /// Returns the SKU text.
    /// </summary>
    /// <returns>The value of <see cref="Value"/>.</returns>
    public override string ToString() => Value;

    private static bool HasValidFormat(string value)
    {
        foreach (char character in value)
        {
            bool isAllowed = char.IsAsciiLetterUpper(c: character) ||
                             char.IsAsciiDigit(c: character) ||
                             character == '-';

            if (!isAllowed)
            {
                return false;
            }
        }

        return true;
    }
}
