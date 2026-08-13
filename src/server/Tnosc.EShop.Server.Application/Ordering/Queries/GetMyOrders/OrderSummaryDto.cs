// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Ordering.Queries.GetMyOrders;

/// <summary>
/// One row of a customer's order list — enough to render a history page without the lines.
/// </summary>
/// <param name="Id">The order's identifier.</param>
/// <param name="OrderNumber">The order's human-facing reference.</param>
/// <param name="Status">The order's status, as its name.</param>
/// <param name="TotalAmount">The order's total after any discount.</param>
/// <param name="TotalCurrency">The three-letter ISO 4217 currency of the total.</param>
/// <param name="PlacedOnUtc">The UTC instant the order was placed.</param>
/// <param name="LineCount">How many lines the order has.</param>
public sealed record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal TotalAmount,
    string TotalCurrency,
    DateTime PlacedOnUtc,
    int LineCount);
