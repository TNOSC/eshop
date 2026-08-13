// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.Queries;

/// <summary>
/// The single row of <see cref="GetOrderSummaryQueryHandler"/>'s raw-SQL result set.
/// </summary>
/// <remarks>
/// Every identifier is declared as <see cref="Guid"/>, never as an <c>OrderId</c>. EF Core value
/// converters apply only to LINQ-translated queries; raw SQL bypasses them entirely, so a typed id
/// here would fail to materialize. Property names match the quoted column aliases in the query one for
/// one.
/// </remarks>
internal sealed class OrderSummaryRow
{
    /// <summary>
    /// Gets or sets the order's identifier.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the order's human-facing reference.
    /// </summary>
    public string OrderNumber { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the customer who placed the order.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the order's status, as its name.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UTC instant the order was placed.
    /// </summary>
    public DateTime PlacedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the order's total after any discount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the currency of the total.
    /// </summary>
    public string TotalCurrency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sum of the lines before any discount.
    /// </summary>
    public decimal SubtotalAmount { get; set; }

    /// <summary>
    /// Gets or sets how many distinct lines the order has.
    /// </summary>
    public long LineCount { get; set; }

    /// <summary>
    /// Gets or sets how many units the order covers across every line.
    /// </summary>
    public long TotalUnits { get; set; }
}
