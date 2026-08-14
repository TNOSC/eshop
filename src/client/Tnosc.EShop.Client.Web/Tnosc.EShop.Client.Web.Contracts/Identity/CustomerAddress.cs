// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Contracts.Identity;

/// <summary>A single address on a customer's profile.</summary>
public sealed record CustomerAddress(Guid Id, string Street, string City, string PostalCode, string Country);
