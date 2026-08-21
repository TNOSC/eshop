// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Identity.Commands.UpdateCustomerAddress;
using Tnosc.Lib.Application.Contexts;

namespace Tnosc.EShop.Server.Api.Identity.UpdateCustomerAddress;

/// <summary>
/// The body of a request replacing one of the caller's own addresses.
/// </summary>
/// <param name="Street">The new street line.</param>
/// <param name="City">The new city.</param>
/// <param name="PostalCode">The new postal code.</param>
/// <param name="Country">The new ISO 3166-1 alpha-2 country code.</param>
internal sealed record UpdateCustomerAddressRequest(
    string? Street,
    string? City,
    string? PostalCode,
    string? Country)
{
    /// <summary>
    /// Composes the command from this body, the route's address identifier and the caller's identity.
    /// </summary>
    /// <param name="userContext">The caller's identity, from their validated token.</param>
    /// <param name="addressId">The identifier of the address to update, from the route.</param>
    /// <returns>The command to hand to the handler.</returns>
    public UpdateCustomerAddressCommand ToCommand(IUserContext userContext, Guid addressId) =>
        new(ExternalUserId: userContext.UserId,
            AddressId: addressId,
            Street: Street,
            City: City,
            PostalCode: PostalCode,
            Country: Country);
}
