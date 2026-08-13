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
/// <strong>Payment columns, added in T14.</strong> <c>payment.payments</c> now exists, carries a
/// unique index over its order id, and the handler's join reflects that — a <c>LEFT JOIN</c> so an
/// order with no payment yet still returns a row, with <see cref="PaymentStatus"/> and
/// <see cref="PaymentMethod"/> both <see langword="null"/>.
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
/// <param name="PaymentStatus">The order's payment's status, as its name, or <see langword="null"/> when none has been initiated.</param>
/// <param name="PaymentMethod">The order's payment's method, as its name, or <see langword="null"/> when none has been initiated.</param>
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
    int TotalUnits,
    string? PaymentStatus,
    string? PaymentMethod);
