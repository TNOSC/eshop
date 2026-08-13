// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Identity.Commands.AdminAddCustomerAddress;

/// <summary>
/// Adds an address to a customer's profile on behalf of an admin acting on a customer other than
/// themselves.
/// </summary>
/// <param name="CustomerId">The identifier of the customer to add the address to, from the route.</param>
/// <param name="Street">The street line.</param>
/// <param name="City">The city.</param>
/// <param name="PostalCode">The postal code.</param>
/// <param name="Country">The ISO 3166-1 alpha-2 country code.</param>
public sealed record AdminAddCustomerAddressCommand(
    Guid CustomerId,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country) : ICommand<Guid>;
