// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Mcp.Application.Products;

namespace Tnosc.EShop.Mcp.Tests.Unit.Products;

/// <summary>
/// Builds a valid <see cref="ProductDetails"/>, overriding only what a test names.
/// </summary>
internal static class ProductDetailsFactory
{
    /// <summary>Creates a product's details with the given identifier.</summary>
    /// <param name="id">The product's identifier.</param>
    /// <returns>A fully populated <see cref="ProductDetails"/>.</returns>
    public static ProductDetails Create(Guid id) =>
        new(
            Id: id,
            Sku: "SKU-0001",
            Name: "Trail Running Shoe",
            Description: "A lightweight shoe for rough terrain.",
            PriceAmount: 129.99m,
            PriceCurrency: "EUR",
            StockQuantity: 12,
            BrandId: Guid.CreateVersion7(),
            BrandName: "Contoso",
            CategoryId: Guid.CreateVersion7(),
            IsDiscontinued: false,
            ImageUrl: null);
}
