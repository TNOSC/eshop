// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Ordering.Queries.GetOrderSummary;

/// <summary>
/// An order's header rolled up with its line statistics — the back-office view of one order.
/// </summary>
/// <remarks>
/// <para>
/// Named <c>…ReportDto</c> rather than <c>OrderSummaryDto</c> because that name is already taken by
/// the far thinner row <c>GetMyOrders</c> returns, and the two are not interchangeable: this one costs
/// a join and an aggregation, and no storefront page should be reaching for it.
/// </para>
/// <para>
/// <strong>Payment columns are not here yet.</strong> The plan has this query joining
/// order × line × payment; the Payment bounded context arrives in T14, so the join it needs has no
/// table to point at today. The order × line half is real and exercised; the payment columns are an
/// additive change to this record and one more <c>LEFT JOIN</c> in the handler when T14 lands, with
/// no consumer of the existing fields affected.
/// </para>
/// </remarks>
/// <param name="OrderId">The order's identifier.</param>
/// <param name="OrderNumber">The order's human-facing reference.</param>
/// <param name="CustomerId">The identifier of the customer who placed the order.</param>
/// <param name="Status">The order's status, as its name.</param>
/// <param name="PlacedOnUtc">The UTC instant the order was placed.</param>
/// <param name="TotalAmount">The order's total after any discount.</param>
/// <param name="TotalCurrency">The three-letter ISO 4217 currency of the total.</param>
/// <param name="SubtotalAmount">The sum of the lines before any discount.</param>
/// <param name="DiscountAmount">What the discount took off — the subtotal less the total.</param>
/// <param name="LineCount">How many distinct lines the order has.</param>
/// <param name="TotalUnits">How many units the order covers across every line.</param>
public sealed record OrderSummaryReportDto(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    string Status,
    DateTime PlacedOnUtc,
    decimal TotalAmount,
    string TotalCurrency,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    int LineCount,
    int TotalUnits);
