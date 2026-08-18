// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.ViewModels;

/// <summary>
/// The caller's basket as loaded for checkout, shown by <c>CheckoutPage</c>. A separate type from
/// <c>Store/Basket</c>'s own basket ViewModel per the per-slice naming rule — read-only display data
/// mapped from <see cref="Tnosc.EShop.Client.Web.Contracts.Basket.Basket"/> by
/// <c>CheckoutService</c>, so it carries no DataAnnotations.
/// </summary>
public sealed class CheckoutBasketViewModel
{
    /// <summary>Gets or sets the basket's lines.</summary>
    public IReadOnlyList<CheckoutBasketItemViewModel> Items { get; init; } = [];

    /// <summary>Gets or sets the basket's total amount.</summary>
    public decimal? TotalAmount { get; init; }

    /// <summary>Gets or sets the basket's total currency.</summary>
    public string? TotalCurrency { get; init; }
}
