// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Contracts.Identity;

/// <summary>The request body to add an address to a customer's profile.</summary>
public sealed record AddCustomerAddressRequest(string? Street, string? City, string? PostalCode, string? Country);
