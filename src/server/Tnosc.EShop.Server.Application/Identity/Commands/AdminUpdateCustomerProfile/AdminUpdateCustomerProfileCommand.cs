// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Identity.Commands.AdminUpdateCustomerProfile;

/// <summary>
/// Updates the parts of a customer's profile this codebase owns — their name and phone number — on
/// behalf of an admin acting on a customer other than themselves.
/// </summary>
/// <remarks>
/// There is deliberately no email here, and no password anywhere. Both belong to Keycloak and are
/// changed in its Account Console; the local email copy is reconciled on the next login.
/// </remarks>
/// <param name="CustomerId">The identifier of the customer to update, from the route.</param>
/// <param name="FirstName">The customer's new given name.</param>
/// <param name="LastName">The customer's new family name.</param>
/// <param name="PhoneNumber">The customer's new contact number, or <see langword="null"/> to clear it.</param>
public sealed record AdminUpdateCustomerProfileCommand(
    Guid CustomerId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber) : ICommand;
