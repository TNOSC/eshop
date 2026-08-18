// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Services;

/// <summary>The back-office landing page's tile counts, one independent result per tile.</summary>
/// <param name="ProductCount">The total product count, when the catalog call succeeded.</param>
/// <param name="ProductsProblem">The catalog call's failure, when it failed.</param>
/// <param name="CustomerCount">The total customer count, when the identity call succeeded.</param>
/// <param name="CustomersProblem">The identity call's failure, when it failed.</param>
public sealed record AdminDashboardCounts(
    long? ProductCount,
    ClientProblem? ProductsProblem,
    long? CustomerCount,
    ClientProblem? CustomersProblem);
