// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Globalization;
using System.Linq;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// A postal address belonging to a <see cref="Customer"/>.
/// </summary>
/// <remarks>
/// <para>
/// Modelled as a child <strong>entity</strong>, not a value object, for two reasons. It needs identity
/// — a caller updates, removes or defaults one particular address out of several, and two addresses
/// with identical text are still two addresses. And a customer holds a <em>collection</em> of them:
/// records compare reference-typed members by reference, so a value object may never expose a mutable
/// collection, which <c>DomainPurityTests.ValueObjects_Should_Be_Sealed_With_No_Mutable_Collection_Properties</c>
/// enforces. The collection therefore lives on the aggregate, which is allowed to hold one.
/// </para>
/// <para>
/// Which address is the customer's default is <em>not</em> a property here: that is a fact about the
/// collection as a whole, so <see cref="Customer"/> owns it. Storing an <c>IsDefault</c> flag per
/// address would make "exactly one default" an invariant spread across every row.
/// </para>
/// </remarks>
public sealed class Address : Entity<AddressId>
{
    /// <summary>
    /// The maximum number of characters a street may contain.
    /// </summary>
    public const int StreetMaxLength = 200;

    /// <summary>
    /// The maximum number of characters a city may contain.
    /// </summary>
    public const int CityMaxLength = 100;

    /// <summary>
    /// The maximum number of characters a postal code may contain.
    /// </summary>
    public const int PostalCodeMaxLength = 20;

    /// <summary>
    /// The exact number of characters an ISO 3166-1 alpha-2 country code contains.
    /// </summary>
    public const int CountryLength = 2;

    private Address()
    {
        // EF.
    }

    /// <summary>
    /// Gets the street line.
    /// </summary>
    public string Street { get; private set; } = null!;

    /// <summary>
    /// Gets the city.
    /// </summary>
    public string City { get; private set; } = null!;

    /// <summary>
    /// Gets the postal code.
    /// </summary>
    public string PostalCode { get; private set; } = null!;

    /// <summary>
    /// Gets the uppercase ISO 3166-1 alpha-2 country code.
    /// </summary>
    public string Country { get; private set; } = null!;

    /// <summary>
    /// Creates an address, validating every part.
    /// </summary>
    /// <remarks>
    /// <see langword="internal"/> because an address only exists as part of a customer — the way in is
    /// <see cref="Customer.AddAddress"/>, which also owns the rules about the collection it joins.
    /// </remarks>
    /// <param name="street">The street line.</param>
    /// <param name="city">The city.</param>
    /// <param name="postalCode">The postal code.</param>
    /// <param name="country">The ISO 3166-1 alpha-2 country code.</param>
    /// <returns>The created address, or one of the <c>Address.*</c> validation errors.</returns>
    internal static Result<Address> Create(
        string? street,
        string? city,
        string? postalCode,
        string? country)
    {
        Result validation = Validate(
            street: street,
            city: city,
            postalCode: postalCode,
            country: country);

        if (validation.IsError)
        {
            return validation.Errors.ToArray();
        }

        return new Address
        {
            Id = AddressId.New(),
            Street = street!.Trim(),
            City = city!.Trim(),
            PostalCode = postalCode!.Trim(),
            Country = country!.Trim().ToUpper(culture: CultureInfo.InvariantCulture),
        };
    }

    /// <summary>
    /// Replaces every part of the address.
    /// </summary>
    /// <remarks>
    /// <see langword="internal"/> for the same reason as <see cref="Create"/>: the way in is
    /// <see cref="Customer.UpdateAddress"/>, which increments the aggregate's version.
    /// </remarks>
    /// <param name="street">The new street line.</param>
    /// <param name="city">The new city.</param>
    /// <param name="postalCode">The new postal code.</param>
    /// <param name="country">The new ISO 3166-1 alpha-2 country code.</param>
    /// <returns>Success, or one of the <c>Address.*</c> validation errors.</returns>
    internal Result Update(
        string? street,
        string? city,
        string? postalCode,
        string? country)
    {
        Result validation = Validate(
            street: street,
            city: city,
            postalCode: postalCode,
            country: country);

        if (validation.IsError)
        {
            return validation.Errors.ToArray();
        }

        Street = street!.Trim();
        City = city!.Trim();
        PostalCode = postalCode!.Trim();
        Country = country!.Trim().ToUpper(culture: CultureInfo.InvariantCulture);

        return Result.Success();
    }

    private static Result Validate(
        string? street,
        string? city,
        string? postalCode,
        string? country)
    {
        if (string.IsNullOrWhiteSpace(value: street))
        {
            return AddressErrors.StreetRequired;
        }

        if (street.Trim().Length > StreetMaxLength)
        {
            return AddressErrors.StreetTooLong;
        }

        if (string.IsNullOrWhiteSpace(value: city))
        {
            return AddressErrors.CityRequired;
        }

        if (city.Trim().Length > CityMaxLength)
        {
            return AddressErrors.CityTooLong;
        }

        if (string.IsNullOrWhiteSpace(value: postalCode))
        {
            return AddressErrors.PostalCodeRequired;
        }

        if (postalCode.Trim().Length > PostalCodeMaxLength)
        {
            return AddressErrors.PostalCodeTooLong;
        }

        if (!IsIsoCountryCode(country: country))
        {
            return AddressErrors.InvalidCountry;
        }

        return Result.Success();
    }

    private static bool IsIsoCountryCode(string? country)
    {
        if (string.IsNullOrWhiteSpace(value: country))
        {
            return false;
        }

        string trimmed = country.Trim();

        return trimmed.Length == CountryLength && trimmed.All(predicate: char.IsAsciiLetter);
    }
}
