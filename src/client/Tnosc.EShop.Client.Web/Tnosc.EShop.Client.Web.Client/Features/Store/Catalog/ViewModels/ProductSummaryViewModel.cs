// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;

/// <summary>
/// A single product row as shown by <c>ProductCard</c> in the storefront gallery. Read-only display
/// data mapped from <see cref="Tnosc.EShop.Client.Web.Contracts.Catalog.ProductSummary"/> by
/// <c>ProductsService</c> — not a form, so it carries no DataAnnotations.
/// </summary>
public sealed class ProductSummaryViewModel
{
    /// <summary>Gets or sets the product id.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets or sets the product's SKU.</summary>
    public string Sku { get; init; } = string.Empty;

    /// <summary>Gets or sets the product's name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets or sets the product's price amount.</summary>
    public decimal PriceAmount { get; init; }

    /// <summary>Gets or sets the product's price currency.</summary>
    public string PriceCurrency { get; init; } = string.Empty;

    /// <summary>Gets or sets the product's stock quantity.</summary>
    public int StockQuantity { get; init; }
}
