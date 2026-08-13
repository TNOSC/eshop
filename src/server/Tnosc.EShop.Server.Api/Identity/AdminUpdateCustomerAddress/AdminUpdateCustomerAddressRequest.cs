// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Identity.Commands.AdminUpdateCustomerAddress;

namespace Tnosc.EShop.Server.Api.Identity.AdminUpdateCustomerAddress;

/// <summary>
/// The body of a request replacing one of a customer's addresses.
/// </summary>
/// <param name="Street">The new street line.</param>
/// <param name="City">The new city.</param>
/// <param name="PostalCode">The new postal code.</param>
/// <param name="Country">The new ISO 3166-1 alpha-2 country code.</param>
internal sealed record AdminUpdateCustomerAddressRequest(
    string? Street,
    string? City,
    string? PostalCode,
    string? Country)
{
    /// <summary>
    /// Composes the command from this body and the route's customer and address identifiers.
    /// </summary>
    /// <param name="customerId">The identifier of the customer, from the route.</param>
    /// <param name="addressId">The identifier of the address to update, from the route.</param>
    /// <returns>The command to hand to the handler.</returns>
    public AdminUpdateCustomerAddressCommand ToCommand(Guid customerId, Guid addressId) =>
        new(CustomerId: customerId,
            AddressId: addressId,
            Street: Street,
            City: City,
            PostalCode: PostalCode,
            Country: Country);
}
