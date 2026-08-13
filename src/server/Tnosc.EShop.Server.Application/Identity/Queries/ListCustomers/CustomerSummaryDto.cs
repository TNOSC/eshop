// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Identity.Queries.ListCustomers;

/// <summary>
/// One row of a customer listing — lighter than <c>CustomerDto</c>, which also carries addresses.
/// </summary>
/// <param name="Id">The customer's identifier.</param>
/// <param name="Email">The customer's email address.</param>
/// <param name="FirstName">The customer's given name.</param>
/// <param name="LastName">The customer's family name.</param>
/// <param name="IsActive">A value indicating whether the customer is still active.</param>
public sealed record CustomerSummaryDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive);
