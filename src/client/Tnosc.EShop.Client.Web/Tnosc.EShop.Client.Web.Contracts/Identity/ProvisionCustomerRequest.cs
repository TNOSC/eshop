// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Contracts.Identity;

/// <summary>The request body to provision the caller's local customer profile.</summary>
public sealed record ProvisionCustomerRequest(string? FirstName, string? LastName, string? PhoneNumber);
