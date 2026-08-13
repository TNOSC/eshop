// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Identity.Commands.AdminRemoveCustomerAddress;

/// <summary>
/// Removes one of a customer's addresses on behalf of an admin.
/// </summary>
/// <remarks>
/// Removing the customer's default address is refused by the aggregate with a conflict — another
/// address has to be made the default first.
/// </remarks>
/// <param name="CustomerId">The identifier of the customer, from the route.</param>
/// <param name="AddressId">The identifier of the address to remove.</param>
public sealed record AdminRemoveCustomerAddressCommand(
    Guid CustomerId,
    Guid AddressId) : ICommand;
