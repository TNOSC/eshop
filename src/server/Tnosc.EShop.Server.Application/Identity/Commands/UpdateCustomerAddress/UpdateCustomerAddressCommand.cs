// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Identity.Commands.UpdateCustomerAddress;

/// <summary>
/// Replaces the contents of one of the caller's own addresses.
/// </summary>
/// <param name="ExternalUserId">The identity provider's subject identifier for the caller, from the token.</param>
/// <param name="AddressId">The identifier of the address to update.</param>
/// <param name="Street">The new street line.</param>
/// <param name="City">The new city.</param>
/// <param name="PostalCode">The new postal code.</param>
/// <param name="Country">The new ISO 3166-1 alpha-2 country code.</param>
public sealed record UpdateCustomerAddressCommand(
    string? ExternalUserId,
    Guid AddressId,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country) : ICommand;
