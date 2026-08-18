// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders.ViewModels;

/// <summary>
/// A single placed order's full detail, as shown by <c>OrderDetailPage</c>. Read-only display data
/// mapped from <see cref="Tnosc.EShop.Client.Web.Contracts.Ordering.Order"/> by
/// <c>OrderDetailService</c> — not a form, so it carries no DataAnnotations.
/// </summary>
public sealed class OrderDetailViewModel
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

    /// <summary>Gets or sets the shipping street line.</summary>
    public string ShippingStreet { get; init; } = string.Empty;

    /// <summary>Gets or sets the shipping city.</summary>
    public string ShippingCity { get; init; } = string.Empty;

    /// <summary>Gets or sets the shipping postal code.</summary>
    public string ShippingPostalCode { get; init; } = string.Empty;

    /// <summary>Gets or sets the shipping country.</summary>
    public string ShippingCountry { get; init; } = string.Empty;

    /// <summary>Gets or sets the order's lines.</summary>
    public IReadOnlyList<OrderLineViewModel> Lines { get; init; } = [];
}
