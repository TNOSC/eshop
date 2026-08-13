// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.EShop.Server.Application.Identity.Commands.AddCustomerAddress;
using Tnosc.Lib.Application.Abstractions.Contexts;

namespace Tnosc.EShop.Server.Api.Identity.AddCustomerAddress;

/// <summary>
/// The body of a request adding an address to the caller's own profile.
/// </summary>
/// <param name="Street">The street line.</param>
/// <param name="City">The city.</param>
/// <param name="PostalCode">The postal code.</param>
/// <param name="Country">The ISO 3166-1 alpha-2 country code.</param>
internal sealed record AddCustomerAddressRequest(
    string? Street,
    string? City,
    string? PostalCode,
    string? Country)
{
    /// <summary>
    /// Composes the command from this body and the caller's identity.
    /// </summary>
    /// <param name="userContext">The caller's identity, from their validated token.</param>
    /// <returns>The command to hand to the handler.</returns>
    public AddCustomerAddressCommand ToCommand(IUserContext userContext) =>
        new(ExternalUserId: userContext.UserId,
            Street: Street,
            City: City,
            PostalCode: PostalCode,
            Country: Country);
}
