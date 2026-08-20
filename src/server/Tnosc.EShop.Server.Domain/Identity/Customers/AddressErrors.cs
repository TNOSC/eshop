// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// Every failure a caller can get back about an <see cref="Address"/>.
/// </summary>
public static class AddressErrors
{
    /// <summary>
    /// Gets the error returned when no street was supplied.
    /// </summary>
    public static Error StreetRequired => Error.Validation(
        code: "Address.StreetRequired",
        description: "A street is required.");

    /// <summary>
    /// Gets the error returned when a street exceeds <see cref="Address.StreetMaxLength"/>.
    /// </summary>
    public static Error StreetTooLong => Error.Validation(
        code: "Address.StreetTooLong",
        description: $"A street must be at most {Address.StreetMaxLength} characters long.");

    /// <summary>
    /// Gets the error returned when no city was supplied.
    /// </summary>
    public static Error CityRequired => Error.Validation(
        code: "Address.CityRequired",
        description: "A city is required.");

    /// <summary>
    /// Gets the error returned when a city exceeds <see cref="Address.CityMaxLength"/>.
    /// </summary>
    public static Error CityTooLong => Error.Validation(
        code: "Address.CityTooLong",
        description: $"A city must be at most {Address.CityMaxLength} characters long.");

    /// <summary>
    /// Gets the error returned when no postal code was supplied.
    /// </summary>
    public static Error PostalCodeRequired => Error.Validation(
        code: "Address.PostalCodeRequired",
        description: "A postal code is required.");

    /// <summary>
    /// Gets the error returned when a postal code exceeds <see cref="Address.PostalCodeMaxLength"/>.
    /// </summary>
    public static Error PostalCodeTooLong => Error.Validation(
        code: "Address.PostalCodeTooLong",
        description: $"A postal code must be at most {Address.PostalCodeMaxLength} characters long.");

    /// <summary>
    /// Gets the error returned when a country is not a two-letter ISO 3166-1 alpha-2 code.
    /// </summary>
    public static Error InvalidCountry => Error.Validation(
        code: "Address.InvalidCountry",
        description: "A country must be a two-letter ISO 3166-1 alpha-2 code.");

    /// <summary>
    /// The customer holds no address with the requested identifier.
    /// </summary>
    /// <param name="addressId">The identifier that was looked up.</param>
    public static Error NotFound(Guid addressId) => Error.NotFound(
        code: "Address.NotFound",
        description: $"Address {addressId} was not found on this customer.");

    /// <summary>
    /// The address is the customer's default and so cannot be removed.
    /// </summary>
    /// <param name="addressId">The identifier of the default address.</param>
    public static Error CannotRemoveDefault(Guid addressId) => Error.Conflict(
        code: "Address.CannotRemoveDefault",
        description: $"Address {addressId} is the customer's default address. Make another address the default before removing it.");
}
