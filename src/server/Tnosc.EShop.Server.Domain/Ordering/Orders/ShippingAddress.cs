// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Globalization;
using System.Linq;
using Tnosc.Lib.Shared.Results;
using Tnosc.Lib.Domain.ValueObjects;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// Where an order is to be delivered, captured by value when the order is placed.
/// </summary>
/// <remarks>
/// A <em>snapshot</em>, and a value object rather than a reference to Identity's <c>Address</c> entity
/// — the same rule an order line follows for the price the customer paid. An order shipped to an
/// address the customer has since edited or deleted must still say where it went; pointing at the live
/// profile would let a profile edit silently rewrite delivery history. That Ordering must not reference
/// Identity is a consequence of the modelling, not the reason for it.
/// </remarks>
public sealed record ShippingAddress : ValueObject
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

    private ShippingAddress(
        string street,
        string city,
        string postalCode,
        string country)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    /// <summary>
    /// Gets the street line.
    /// </summary>
    public string Street { get; }

    /// <summary>
    /// Gets the city.
    /// </summary>
    public string City { get; }

    /// <summary>
    /// Gets the postal code.
    /// </summary>
    public string PostalCode { get; }

    /// <summary>
    /// Gets the uppercase ISO 3166-1 alpha-2 country code.
    /// </summary>
    public string Country { get; }

    /// <summary>
    /// Creates a <see cref="ShippingAddress"/>, validating every part.
    /// </summary>
    /// <param name="street">The street line.</param>
    /// <param name="city">The city.</param>
    /// <param name="postalCode">The postal code.</param>
    /// <param name="country">The ISO 3166-1 alpha-2 country code.</param>
    /// <returns>The created address, or one of the <c>ShippingAddress.*</c> validation errors.</returns>
    public static Result<ShippingAddress> Create(
        string? street,
        string? city,
        string? postalCode,
        string? country)
    {
        if (string.IsNullOrWhiteSpace(value: street) || street.Trim().Length > StreetMaxLength)
        {
            return ShippingAddressErrors.InvalidStreet;
        }

        if (string.IsNullOrWhiteSpace(value: city) || city.Trim().Length > CityMaxLength)
        {
            return ShippingAddressErrors.InvalidCity;
        }

        if (string.IsNullOrWhiteSpace(value: postalCode) || postalCode.Trim().Length > PostalCodeMaxLength)
        {
            return ShippingAddressErrors.InvalidPostalCode;
        }

        if (!IsIsoCountryCode(country: country))
        {
            return ShippingAddressErrors.InvalidCountry;
        }

        return new ShippingAddress(
            street: street.Trim(),
            city: city.Trim(),
            postalCode: postalCode.Trim(),
            country: country!.Trim().ToUpper(culture: CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Returns the address on one line.
    /// </summary>
    /// <returns>The street, city, postal code and country, comma-separated.</returns>
    public override string ToString() =>
        $"{Street}, {City}, {PostalCode}, {Country}";

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
