// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using Tnosc.Lib.Domain.Results;
using Tnosc.Lib.Domain.ValueObjects;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// A customer's contact number in E.164 form.
/// </summary>
/// <remarks>
/// Optional on a <see cref="Customer"/>: the property is nullable, and callers pass
/// <see langword="null"/> rather than an empty <see cref="PhoneNumber"/> to mean "none". Storing
/// E.164 keeps the value unambiguous across countries without needing to know the caller's locale.
/// </remarks>
public sealed record PhoneNumber : ValueObject
{
    /// <summary>
    /// The fewest digits an E.164 number may carry, excluding the leading '+'.
    /// </summary>
    public const int MinDigits = 7;

    /// <summary>
    /// The most digits an E.164 number may carry, excluding the leading '+'.
    /// </summary>
    public const int MaxDigits = 15;

    /// <summary>
    /// The maximum number of characters the stored value may contain, including the leading '+'.
    /// </summary>
    public const int MaxLength = MaxDigits + 1;

    private PhoneNumber(string value) => Value = value;

    /// <summary>
    /// Gets the phone number in E.164 form.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a <see cref="PhoneNumber"/>, validating that it is in E.164 form.
    /// </summary>
    /// <param name="value">The candidate phone number. Spaces, dashes and parentheses are stripped first.</param>
    /// <returns>
    /// The created <see cref="PhoneNumber"/>, or <c>PhoneNumber.Empty</c> /
    /// <c>PhoneNumber.InvalidFormat</c>.
    /// </returns>
    public static Result<PhoneNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value: value))
        {
            return PhoneNumberErrors.Empty;
        }

        string normalized = new([.. value.Where(predicate: static character =>
            character is not (' ' or '-' or '(' or ')' or '.'))]);

        if (normalized.Length < MinDigits + 1 || normalized.Length > MaxLength || normalized[0] != '+')
        {
            return PhoneNumberErrors.InvalidFormat;
        }

        if (!normalized[1..].All(predicate: char.IsAsciiDigit))
        {
            return PhoneNumberErrors.InvalidFormat;
        }

        return new PhoneNumber(value: normalized);
    }

    /// <summary>
    /// Returns the phone number.
    /// </summary>
    /// <returns>The value of <see cref="Value"/>.</returns>
    public override string ToString() => Value;
}
