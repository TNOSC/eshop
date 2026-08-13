// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Identity.Commands.UpdateCustomerProfile;

/// <summary>
/// Updates the parts of a customer's profile this codebase owns: their name and phone number.
/// </summary>
/// <remarks>
/// There is deliberately no email here, and no password anywhere. Both belong to Keycloak and are
/// changed in its Account Console; the local email copy is reconciled on the next login.
/// </remarks>
/// <param name="ExternalUserId">
/// The identity provider's subject identifier for the caller, taken from the token by the endpoint.
/// Passed as data so the handler stays testable without an HTTP context.
/// </param>
/// <param name="FirstName">The customer's new given name.</param>
/// <param name="LastName">The customer's new family name.</param>
/// <param name="PhoneNumber">The customer's new contact number, or <see langword="null"/> to clear it.</param>
public sealed record UpdateCustomerProfileCommand(
    string? ExternalUserId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber) : ICommand;
