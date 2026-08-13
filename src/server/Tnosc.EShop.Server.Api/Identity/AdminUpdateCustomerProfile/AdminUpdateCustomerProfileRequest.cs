// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Identity.Commands.AdminUpdateCustomerProfile;

namespace Tnosc.EShop.Server.Api.Identity.AdminUpdateCustomerProfile;

/// <summary>
/// The body of a profile update: the name and phone number this codebase owns.
/// </summary>
/// <remarks>
/// No email and no password field, deliberately — both belong to Keycloak.
/// </remarks>
/// <param name="FirstName">The customer's new given name.</param>
/// <param name="LastName">The customer's new family name.</param>
/// <param name="PhoneNumber">The customer's new contact number, or omitted to clear it.</param>
internal sealed record AdminUpdateCustomerProfileRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber)
{
    /// <summary>
    /// Composes the command from this body and the target customer identifier taken from the route.
    /// </summary>
    /// <param name="customerId">The identifier of the customer to update, from the route.</param>
    /// <returns>The command to hand to the handler.</returns>
    public AdminUpdateCustomerProfileCommand ToCommand(Guid customerId) =>
        new(CustomerId: customerId,
            FirstName: FirstName,
            LastName: LastName,
            PhoneNumber: string.IsNullOrWhiteSpace(value: PhoneNumber) ? null : PhoneNumber);
}
