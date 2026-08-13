// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Security.Cryptography;
using Tnosc.Lib.Domain.Results;
using Tnosc.Lib.Domain.ValueObjects;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// The human-facing reference for an order — the value a customer quotes to support, as opposed to
/// the <see cref="OrderId"/> the system joins on.
/// </summary>
/// <remarks>
/// Two identifiers rather than one on purpose. <see cref="OrderId"/> is a GUID: unguessable, stable,
/// and useless to read aloud. This is short, sortable by eye and safe to print, but it is not the key
/// anything joins on, so its shape can change for orders placed later without touching a foreign key.
/// </remarks>
public sealed record OrderNumber : ValueObject
{
    /// <summary>
    /// The prefix every order number carries.
    /// </summary>
    public const string Prefix = "ORD";

    /// <summary>
    /// The exact number of characters a generated order number contains — <c>ORD-yyyyMMdd-XXXXXX</c>:
    /// a three-character prefix, a dash, eight date digits, a dash, and a six-character suffix.
    /// </summary>
    public const int Length = 19;

    /// <summary>
    /// The number of random characters closing a generated order number.
    /// </summary>
    public const int SuffixLength = 6;

    private const string SuffixAlphabet = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    private OrderNumber(string value) => Value = value;

    /// <summary>
    /// Gets the order number.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates an <see cref="OrderNumber"/> from an existing value, validating its shape.
    /// </summary>
    /// <param name="value">The order number to wrap.</param>
    /// <returns>
    /// The created <see cref="OrderNumber"/>, or an <c>OrderNumber.Required</c> /
    /// <c>OrderNumber.InvalidFormat</c> validation error.
    /// </returns>
    public static Result<OrderNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value: value))
        {
            return OrderNumberErrors.Required;
        }

        string trimmed = value.Trim();

        if (!IsWellFormed(value: trimmed))
        {
            return OrderNumberErrors.InvalidFormat;
        }

        return new OrderNumber(value: trimmed);
    }

    /// <summary>
    /// Generates a fresh order number for the supplied instant.
    /// </summary>
    /// <remarks>
    /// The date segment makes the value sortable and human-meaningful; the random suffix is what keeps
    /// two orders placed in the same second apart. Collisions are not left to chance alone — the
    /// <c>ux_orders_order_number</c> unique index is the physical backstop.
    /// </remarks>
    /// <param name="placedOnUtc">The instant the order was placed.</param>
    /// <returns>A new <see cref="OrderNumber"/>.</returns>
    public static OrderNumber Generate(DateTime placedOnUtc)
    {
        // Cryptographic randomness, not Random.Shared: an order number is quoted in emails and support
        // tickets, so a guessable suffix would let one customer's reference be extrapolated from
        // another's.
        string suffix = RandomNumberGenerator.GetString(choices: SuffixAlphabet, length: SuffixLength);
        string date = placedOnUtc.ToString(format: "yyyyMMdd", provider: CultureInfo.InvariantCulture);

        return new OrderNumber(value: $"{Prefix}-{date}-{suffix}");
    }

    /// <summary>
    /// Returns the order number.
    /// </summary>
    /// <returns>The value of <see cref="Value"/>.</returns>
    public override string ToString() => Value;

    private static bool IsWellFormed(string value)
    {
        if (value.Length != Length)
        {
            return false;
        }

        if (!value.StartsWith(value: $"{Prefix}-", comparisonType: StringComparison.Ordinal))
        {
            return false;
        }

        if (value[^(SuffixLength + 1)] != '-')
        {
            return false;
        }

        foreach (char character in value.AsSpan(start: Prefix.Length + 1, length: 8))
        {
            if (!char.IsAsciiDigit(c: character))
            {
                return false;
            }
        }

        foreach (char character in value.AsSpan(start: value.Length - SuffixLength))
        {
            if (!char.IsAsciiLetterUpper(c: character) && !char.IsAsciiDigit(c: character))
            {
                return false;
            }
        }

        return true;
    }
}
