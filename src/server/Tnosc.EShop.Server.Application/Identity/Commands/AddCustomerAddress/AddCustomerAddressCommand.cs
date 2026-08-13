// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Identity.Commands.AddCustomerAddress;

/// <summary>
/// Adds an address to the caller's own customer profile.
/// </summary>
/// <param name="ExternalUserId">The identity provider's subject identifier for the caller, from the token.</param>
/// <param name="Street">The street line.</param>
/// <param name="City">The city.</param>
/// <param name="PostalCode">The postal code.</param>
/// <param name="Country">The ISO 3166-1 alpha-2 country code.</param>
public sealed record AddCustomerAddressCommand(
    string? ExternalUserId,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country) : ICommand<Guid>;
