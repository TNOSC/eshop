// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Identity.Commands.SetDefaultCustomerAddress;

/// <summary>
/// Makes one of the caller's own addresses their default.
/// </summary>
/// <param name="ExternalUserId">The identity provider's subject identifier for the caller, from the token.</param>
/// <param name="AddressId">The identifier of the address to make default.</param>
public sealed record SetDefaultCustomerAddressCommand(
    string? ExternalUserId,
    Guid AddressId) : ICommand;
