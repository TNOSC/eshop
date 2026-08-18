// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Basket.ViewModels;

/// <summary>
/// A single line in the caller's basket, nested under <see cref="BasketViewModel"/> and shown by
/// <c>BasketLineRow</c>. Read-only display data mapped from
/// <see cref="Tnosc.EShop.Client.Web.Contracts.Basket.BasketItem"/> by <c>BasketPageService</c> — not
/// a form, so it carries no DataAnnotations.
/// </summary>
public sealed class BasketItemViewModel
{
    /// <summary>Gets or sets the basket item id.</summary>
    public Guid ItemId { get; init; }

    /// <summary>Gets or sets the product's SKU.</summary>
    public string Sku { get; init; } = string.Empty;

    /// <summary>Gets or sets the product's name.</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>Gets or sets the unit price amount.</summary>
    public decimal UnitPriceAmount { get; init; }

    /// <summary>Gets or sets the unit price currency.</summary>
    public string UnitPriceCurrency { get; init; } = string.Empty;

    /// <summary>Gets or sets the ordered quantity.</summary>
    public int Quantity { get; init; }
}
