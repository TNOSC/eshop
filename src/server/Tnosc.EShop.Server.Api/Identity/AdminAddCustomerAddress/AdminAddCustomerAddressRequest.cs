// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Identity.Commands.AdminAddCustomerAddress;

namespace Tnosc.EShop.Server.Api.Identity.AdminAddCustomerAddress;

/// <summary>
/// The body of a request adding an address to a customer's profile.
/// </summary>
/// <param name="Street">The street line.</param>
/// <param name="City">The city.</param>
/// <param name="PostalCode">The postal code.</param>
/// <param name="Country">The ISO 3166-1 alpha-2 country code.</param>
internal sealed record AdminAddCustomerAddressRequest(
    string? Street,
    string? City,
    string? PostalCode,
    string? Country)
{
    /// <summary>
    /// Composes the command from this body and the target customer identifier taken from the route.
    /// </summary>
    /// <param name="customerId">The identifier of the customer to add the address to, from the route.</param>
    /// <returns>The command to hand to the handler.</returns>
    public AdminAddCustomerAddressCommand ToCommand(Guid customerId) =>
        new(CustomerId: customerId,
            Street: Street,
            City: City,
            PostalCode: PostalCode,
            Country: Country);
}
