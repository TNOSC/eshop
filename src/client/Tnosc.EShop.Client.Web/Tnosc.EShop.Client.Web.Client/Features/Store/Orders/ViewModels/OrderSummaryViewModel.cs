// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders.ViewModels;

/// <summary>
/// A single order row as shown by <c>MyOrdersPage</c>'s order history listing. Read-only display data
/// mapped from <see cref="Tnosc.EShop.Client.Web.Contracts.Ordering.OrderSummary"/> by
/// <c>MyOrdersService</c> — not a form, so it carries no DataAnnotations.
/// </summary>
public sealed class OrderSummaryViewModel
{
    /// <summary>Gets or sets the order id.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets or sets the order number.</summary>
    public string OrderNumber { get; init; } = string.Empty;

    /// <summary>Gets or sets the order's status, as the server's plain wire string.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Gets or sets the order's total amount.</summary>
    public decimal TotalAmount { get; init; }

    /// <summary>Gets or sets the order's total currency.</summary>
    public string TotalCurrency { get; init; } = string.Empty;

    /// <summary>Gets or sets the moment the order was placed, in UTC.</summary>
    public DateTime PlacedOnUtc { get; init; }
}
