// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Tnosc.EShop.Client.Web.Contracts.Identity;

/// <summary>The full detail of a single customer's profile.</summary>
public sealed record Customer(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    bool IsActive,
    Guid? DefaultAddressId,
    IReadOnlyList<CustomerAddress> Addresses);
