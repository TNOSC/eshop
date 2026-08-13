// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Identity.Commands.RemoveCustomerAddress;

/// <summary>
/// Removes one of the caller's own addresses.
/// </summary>
/// <remarks>
/// Removing the customer's default address is refused by the aggregate with a conflict — another
/// address has to be made the default first.
/// </remarks>
/// <param name="ExternalUserId">The identity provider's subject identifier for the caller, from the token.</param>
/// <param name="AddressId">The identifier of the address to remove.</param>
public sealed record RemoveCustomerAddressCommand(
    string? ExternalUserId,
    Guid AddressId) : ICommand;
