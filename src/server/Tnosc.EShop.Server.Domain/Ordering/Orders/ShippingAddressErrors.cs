// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// Every failure a caller can get back about a <see cref="ShippingAddress"/>, defined once.
/// </summary>
public static class ShippingAddressErrors
{
    /// <summary>
    /// Gets the error returned when the street is missing or too long.
    /// </summary>
    public static Error InvalidStreet => Error.Validation(
        code: "ShippingAddress.InvalidStreet",
        description: $"A shipping street is required and must be at most {ShippingAddress.StreetMaxLength} characters long.");

    /// <summary>
    /// Gets the error returned when the city is missing or too long.
    /// </summary>
    public static Error InvalidCity => Error.Validation(
        code: "ShippingAddress.InvalidCity",
        description: $"A shipping city is required and must be at most {ShippingAddress.CityMaxLength} characters long.");

    /// <summary>
    /// Gets the error returned when the postal code is missing or too long.
    /// </summary>
    public static Error InvalidPostalCode => Error.Validation(
        code: "ShippingAddress.InvalidPostalCode",
        description: $"A shipping postal code is required and must be at most {ShippingAddress.PostalCodeMaxLength} characters long.");

    /// <summary>
    /// Gets the error returned when the country is not an ISO 3166-1 alpha-2 code.
    /// </summary>
    public static Error InvalidCountry => Error.Validation(
        code: "ShippingAddress.InvalidCountry",
        description: "A shipping country must be a two-letter ISO 3166-1 alpha-2 code.");
}
