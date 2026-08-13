// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Identity.Commands.ProvisionCustomer;

/// <summary>
/// Provisions the local customer profile for an identity-provider account, registering one on first
/// login and reconciling the existing one on every later login.
/// </summary>
/// <remarks>
/// <see cref="ExternalUserId"/> and <see cref="Email"/> come from the caller's token, not from the
/// request body — the endpoint reads them off <c>IUserContext</c>. The name and phone number are the
/// only parts the client supplies, because <c>IUserContext</c> exposes no name.
/// </remarks>
/// <param name="ExternalUserId">The identity provider's subject identifier for the caller.</param>
/// <param name="Email">The caller's email address, as the identity provider currently holds it.</param>
/// <param name="FirstName">The caller's given name.</param>
/// <param name="LastName">The caller's family name.</param>
/// <param name="PhoneNumber">The caller's optional contact number.</param>
public sealed record ProvisionCustomerCommand(
    string? ExternalUserId,
    string? Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber) : ICommand<ProvisionCustomerResult>;
