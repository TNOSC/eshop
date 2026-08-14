// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Tests.Acceptance.Contracts;

/// <summary>
/// An order, as <c>GET /api/orders/{id}</c> returns it.
/// </summary>
/// <param name="Id">The order's identifier.</param>
/// <param name="OrderNumber">The human-facing order number.</param>
/// <param name="Status">The order's current status.</param>
/// <param name="TotalAmount">The order's total.</param>
/// <param name="TotalCurrency">The currency the total is denominated in.</param>
public sealed record Order(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal TotalAmount,
    string TotalCurrency);
