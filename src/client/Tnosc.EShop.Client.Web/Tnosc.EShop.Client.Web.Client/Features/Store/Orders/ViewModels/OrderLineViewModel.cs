// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders.ViewModels;

/// <summary>
/// A single line of an order, nested under <see cref="OrderDetailViewModel"/>. Read-only display
/// data mapped from <see cref="Tnosc.EShop.Client.Web.Contracts.Ordering.OrderLine"/> by
/// <c>OrderDetailService</c> — not a form, so it carries no DataAnnotations.
/// </summary>
public sealed class OrderLineViewModel
{
    /// <summary>Gets or sets the order line id.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets or sets the product's SKU.</summary>
    public string Sku { get; init; } = string.Empty;

    /// <summary>Gets or sets the product's name.</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>Gets or sets the unit price's currency.</summary>
    public string UnitPriceCurrency { get; init; } = string.Empty;

    /// <summary>Gets or sets the ordered quantity.</summary>
    public int Quantity { get; init; }

    /// <summary>Gets or sets the line's total amount.</summary>
    public decimal LineTotalAmount { get; init; }
}
