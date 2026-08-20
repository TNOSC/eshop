// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Shared.Results;
using Tnosc.Lib.Domain.ValueObjects;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// The identity provider's own identifier for a user — Keycloak's <c>sub</c> claim.
/// </summary>
/// <remarks>
/// <para>
/// This is the join between a Keycloak account and the local <see cref="Customer"/> profile, and it
/// is what makes provisioning exactly-once without an idempotency key: a second login presents the
/// same <c>sub</c>, so <see cref="CustomerFactory.ProvisionAsync"/> finds the existing customer and
/// reconciles rather than registering a second one.
/// </para>
/// <para>
/// Deliberately a string rather than a <see cref="System.Guid"/>: <c>sub</c> is an opaque token per
/// OpenID Connect, and Keycloak's happening to issue a UUID today is not a contract to model against.
/// </para>
/// </remarks>
public sealed record ExternalUserId : ValueObject
{
    /// <summary>
    /// The maximum number of characters an external user identifier may contain.
    /// </summary>
    public const int MaxLength = 200;

    private ExternalUserId(string value) => Value = value;

    /// <summary>
    /// Gets the identity provider's subject identifier.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates an <see cref="ExternalUserId"/>, validating that it is present and bounded.
    /// </summary>
    /// <param name="value">The candidate subject identifier.</param>
    /// <returns>
    /// The created <see cref="ExternalUserId"/>, or <c>ExternalUserId.Empty</c> /
    /// <c>ExternalUserId.TooLong</c>.
    /// </returns>
    public static Result<ExternalUserId> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value: value))
        {
            return ExternalUserIdErrors.Empty;
        }

        string trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            return ExternalUserIdErrors.TooLong;
        }

        return new ExternalUserId(value: trimmed);
    }

    /// <summary>
    /// Returns the subject identifier.
    /// </summary>
    /// <returns>The value of <see cref="Value"/>.</returns>
    public override string ToString() => Value;
}
