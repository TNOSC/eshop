// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Shared.Results;
using Tnosc.Lib.Domain.ValueObjects;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// A customer's given and family name.
/// </summary>
/// <remarks>
/// Owned by this codebase rather than by Keycloak. The client passes the token's <c>given_name</c> and
/// <c>family_name</c> through on first provisioning, but from then on the customer edits their name
/// here — unlike their email, which is reconciled from the identity provider on every login.
/// </remarks>
public sealed record PersonName : ValueObject
{
    /// <summary>
    /// The maximum number of characters either name part may contain.
    /// </summary>
    public const int MaxPartLength = 100;

    private PersonName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    /// <summary>
    /// Gets the customer's given name.
    /// </summary>
    public string FirstName { get; }

    /// <summary>
    /// Gets the customer's family name.
    /// </summary>
    public string LastName { get; }

    /// <summary>
    /// Creates a <see cref="PersonName"/>, validating that both parts are present and bounded.
    /// </summary>
    /// <param name="firstName">The candidate given name.</param>
    /// <param name="lastName">The candidate family name.</param>
    /// <returns>
    /// The created <see cref="PersonName"/>, or one of the <c>PersonName.*Required</c> /
    /// <c>PersonName.*TooLong</c> validation errors.
    /// </returns>
    public static Result<PersonName> Create(string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(value: firstName))
        {
            return PersonNameErrors.FirstNameRequired;
        }

        if (string.IsNullOrWhiteSpace(value: lastName))
        {
            return PersonNameErrors.LastNameRequired;
        }

        string trimmedFirstName = firstName.Trim();
        string trimmedLastName = lastName.Trim();

        if (trimmedFirstName.Length > MaxPartLength)
        {
            return PersonNameErrors.FirstNameTooLong;
        }

        if (trimmedLastName.Length > MaxPartLength)
        {
            return PersonNameErrors.LastNameTooLong;
        }

        return new PersonName(firstName: trimmedFirstName, lastName: trimmedLastName);
    }

    /// <summary>
    /// Returns the customer's full name.
    /// </summary>
    /// <returns>The given and family names, separated by a space.</returns>
    public override string ToString() => $"{FirstName} {LastName}";
}
