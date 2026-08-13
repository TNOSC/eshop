// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Identity.Queries.GetCurrentCustomer;

/// <summary>
/// One of a customer's addresses, as callers see it.
/// </summary>
/// <remarks>
/// Whether this address is the customer's default is not a field here: it is a fact about the
/// collection, carried once by <see cref="CustomerDto.DefaultAddressId"/>, exactly as the aggregate
/// models it.
/// </remarks>
/// <param name="Id">The address's identifier.</param>
/// <param name="Street">The street line.</param>
/// <param name="City">The city.</param>
/// <param name="PostalCode">The postal code.</param>
/// <param name="Country">The ISO 3166-1 alpha-2 country code.</param>
public sealed record CustomerAddressDto(
    Guid Id,
    string Street,
    string City,
    string PostalCode,
    string Country);
