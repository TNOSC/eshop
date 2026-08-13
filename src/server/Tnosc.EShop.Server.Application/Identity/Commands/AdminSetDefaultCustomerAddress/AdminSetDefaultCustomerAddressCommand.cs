// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Identity.Commands.AdminSetDefaultCustomerAddress;

/// <summary>
/// Makes one of a customer's addresses their default, on behalf of an admin.
/// </summary>
/// <param name="CustomerId">The identifier of the customer, from the route.</param>
/// <param name="AddressId">The identifier of the address to make default.</param>
public sealed record AdminSetDefaultCustomerAddressCommand(
    Guid CustomerId,
    Guid AddressId) : ICommand;
