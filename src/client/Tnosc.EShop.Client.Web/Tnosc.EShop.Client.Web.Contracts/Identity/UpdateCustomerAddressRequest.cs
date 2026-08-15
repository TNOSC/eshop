// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Contracts.Identity;

/// <summary>The request body to replace one of a customer's addresses.</summary>
public sealed record UpdateCustomerAddressRequest(string? Street, string? City, string? PostalCode, string? Country);
