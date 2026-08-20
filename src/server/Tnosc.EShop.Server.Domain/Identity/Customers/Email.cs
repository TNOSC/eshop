// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Globalization;
using Tnosc.Lib.Shared.Results;
using Tnosc.Lib.Domain.ValueObjects;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// A customer's email address, normalised to lowercase.
/// </summary>
/// <remarks>
/// <para>
/// Keycloak is the source of truth for this value; the local copy is reconciled from the token claim
/// on every login by <see cref="Customer.SyncEmail"/> and is never edited through this API.
/// </para>
/// <para>
/// Normalising to lowercase at construction is what makes uniqueness meaningful: without it,
/// <c>Sami@Example.com</c> and <c>sami@example.com</c> would be two different customers, both to the
/// domain check in <see cref="CustomerFactory"/> and to the unique index backing it.
/// </para>
/// </remarks>
public sealed record Email : ValueObject
{
    /// <summary>
    /// The maximum number of characters an email address may contain.
    /// </summary>
    public const int MaxLength = 320;

    private Email(string value) => Value = value;

    /// <summary>
    /// Gets the normalised, lowercase email address.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates an <see cref="Email"/>, validating its format and normalising it to lowercase.
    /// </summary>
    /// <param name="value">The candidate email address.</param>
    /// <returns>
    /// The created <see cref="Email"/>, or <c>Email.Empty</c> / <c>Email.TooLong</c> /
    /// <c>Email.InvalidFormat</c> when <paramref name="value"/> breaks the format invariant.
    /// </returns>
    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value: value))
        {
            return EmailErrors.Empty;
        }

#pragma warning disable CA1308 // Lowercase is the canonical, user-facing form of an email address and the form Keycloak issues; this value is stored and displayed, not folded for comparison. InvariantCulture already answers the rule's culture concern.
        string normalized = value.Trim().ToLower(culture: CultureInfo.InvariantCulture);
#pragma warning restore CA1308

        if (normalized.Length > MaxLength)
        {
            return EmailErrors.TooLong;
        }

        if (!HasValidFormat(value: normalized))
        {
            return EmailErrors.InvalidFormat;
        }

        return new Email(value: normalized);
    }

    /// <summary>
    /// Returns the normalised email address.
    /// </summary>
    /// <returns>The value of <see cref="Value"/>.</returns>
    public override string ToString() => Value;

    // Deliberately structural rather than an RFC 5322 regex: the identity provider has already
    // accepted and, where configured, verified this address. This guards against a malformed value
    // reaching the database, it does not attempt to re-adjudicate deliverability.
    private static bool HasValidFormat(string value)
    {
        int atIndex = value.IndexOf(value: '@', comparisonType: StringComparison.Ordinal);

        if (atIndex <= 0 || atIndex != value.LastIndexOf(value: '@'))
        {
            return false;
        }

        string domain = value[(atIndex + 1)..];

        return domain.Length > 0 &&
               domain.Contains(value: '.', comparisonType: StringComparison.Ordinal) &&
               !domain.StartsWith(value: '.') &&
               !domain.EndsWith(value: '.') &&
               !value.Contains(value: ' ', comparisonType: StringComparison.Ordinal);
    }
}
