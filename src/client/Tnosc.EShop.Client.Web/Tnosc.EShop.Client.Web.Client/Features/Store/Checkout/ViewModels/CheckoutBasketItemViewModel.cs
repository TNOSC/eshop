// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.ViewModels;

/// <summary>
/// A single line of the caller's basket, nested under <see cref="CheckoutBasketViewModel"/> and shown
/// by <c>CheckoutPage</c>. A separate type from <c>Store/Basket</c>'s own basket-item ViewModel per
/// the per-slice naming rule — read-only display data mapped from
/// <see cref="Tnosc.EShop.Client.Web.Contracts.Basket.BasketItem"/> by <c>CheckoutService</c>, so it
/// carries no DataAnnotations.
/// </summary>
public sealed class CheckoutBasketItemViewModel
{
    /// <summary>Gets or sets the basket item id.</summary>
    public Guid ItemId { get; init; }

    /// <summary>Gets or sets the product's name.</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>Gets or sets the unit price amount.</summary>
    public decimal UnitPriceAmount { get; init; }

    /// <summary>Gets or sets the unit price currency.</summary>
    public string UnitPriceCurrency { get; init; } = string.Empty;

    /// <summary>Gets or sets the ordered quantity.</summary>
    public int Quantity { get; init; }
}
