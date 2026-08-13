// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Identity.Commands.AdminUpdateCustomerAddress;

/// <summary>
/// Replaces the contents of one of a customer's addresses on behalf of an admin.
/// </summary>
/// <param name="CustomerId">The identifier of the customer, from the route.</param>
/// <param name="AddressId">The identifier of the address to update.</param>
/// <param name="Street">The new street line.</param>
/// <param name="City">The new city.</param>
/// <param name="PostalCode">The new postal code.</param>
/// <param name="Country">The new ISO 3166-1 alpha-2 country code.</param>
public sealed record AdminUpdateCustomerAddressCommand(
    Guid CustomerId,
    Guid AddressId,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country) : ICommand;
