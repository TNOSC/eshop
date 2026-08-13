// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.EShop.Server.Application.Identity.Commands.UpdateCustomerProfile;
using Tnosc.Lib.Application.Abstractions.Contexts;

namespace Tnosc.EShop.Server.Api.Identity.UpdateCustomerProfile;

/// <summary>
/// The body of a profile update: the name and phone number this codebase owns.
/// </summary>
/// <remarks>
/// No email and no password field, deliberately — both belong to Keycloak.
/// </remarks>
/// <param name="FirstName">The caller's new given name.</param>
/// <param name="LastName">The caller's new family name.</param>
/// <param name="PhoneNumber">The caller's new contact number, or omitted to clear it.</param>
internal sealed record UpdateCustomerProfileRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber)
{
    /// <summary>
    /// Composes the command from this body and the caller's identity.
    /// </summary>
    /// <param name="userContext">The caller's identity, from their validated token.</param>
    /// <returns>The command to hand to the handler.</returns>
    public UpdateCustomerProfileCommand ToCommand(IUserContext userContext) =>
        new(ExternalUserId: userContext.UserId,
            FirstName: FirstName,
            LastName: LastName,
            PhoneNumber: string.IsNullOrWhiteSpace(value: PhoneNumber) ? null : PhoneNumber);
}
