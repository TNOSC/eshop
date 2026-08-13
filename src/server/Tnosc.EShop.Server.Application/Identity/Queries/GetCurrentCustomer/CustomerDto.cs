// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Tnosc.EShop.Server.Application.Identity.Queries.GetCurrentCustomer;

/// <summary>
/// A customer profile as callers see it.
/// </summary>
/// <remarks>
/// Shared by both Identity queries — the caller's own profile and an operator's read of any
/// customer — because the shape is identical; only who may ask differs, which is an authorization
/// concern rather than a projection one.
/// </remarks>
/// <param name="Id">The customer's identifier.</param>
/// <param name="Email">The customer's email address, as reconciled from the identity provider.</param>
/// <param name="FirstName">The customer's given name.</param>
/// <param name="LastName">The customer's family name.</param>
/// <param name="PhoneNumber">The customer's contact number, if they have supplied one.</param>
/// <param name="IsActive">Whether the customer is still active.</param>
/// <param name="DefaultAddressId">The identifier of the customer's default address, if they hold any.</param>
/// <param name="Addresses">The customer's addresses.</param>
public sealed record CustomerDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    bool IsActive,
    Guid? DefaultAddressId,
    IReadOnlyCollection<CustomerAddressDto> Addresses);
