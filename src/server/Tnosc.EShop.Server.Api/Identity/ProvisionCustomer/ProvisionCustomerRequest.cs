// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.EShop.Server.Application.Identity.Commands.ProvisionCustomer;
using Tnosc.Lib.Application.Abstractions.Contexts;

namespace Tnosc.EShop.Server.Api.Identity.ProvisionCustomer;

/// <summary>
/// The body of a provisioning request: only the parts of the profile the token does not carry.
/// </summary>
/// <remarks>
/// <c>sub</c> and <c>email</c> are never accepted from the body — they come from the caller's token
/// via <see cref="IUserContext"/>, so a caller cannot provision a profile for someone else. The name
/// is here because <see cref="IUserContext"/> deliberately exposes none, and reading
/// <c>ClaimsPrincipal</c> in an endpoint is not allowed; the client already holds <c>given_name</c>
/// and <c>family_name</c> from the same token and passes them through.
/// </remarks>
/// <param name="FirstName">The caller's given name.</param>
/// <param name="LastName">The caller's family name.</param>
/// <param name="PhoneNumber">The caller's optional contact number.</param>
internal sealed record ProvisionCustomerRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber)
{
    /// <summary>
    /// Composes the command from this body and the caller's identity.
    /// </summary>
    /// <param name="userContext">The caller's identity, from their validated token.</param>
    /// <returns>The command to hand to the handler.</returns>
    public ProvisionCustomerCommand ToCommand(IUserContext userContext) =>
        new(ExternalUserId: userContext.UserId,
            Email: userContext.Email,
            FirstName: FirstName,
            LastName: LastName,
            // Blank is normalised to "no number" here rather than in the handler: whether an empty
            // form field means "none" is a wire-format concern, not a domain rule.
            PhoneNumber: string.IsNullOrWhiteSpace(value: PhoneNumber) ? null : PhoneNumber);
}
