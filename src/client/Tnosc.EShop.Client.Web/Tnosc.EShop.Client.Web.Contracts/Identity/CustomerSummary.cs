// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Contracts.Identity;

/// <summary>A single customer row as shown in an admin listing.</summary>
public sealed record CustomerSummary(Guid Id, string Email, string FirstName, string LastName, bool IsActive);
