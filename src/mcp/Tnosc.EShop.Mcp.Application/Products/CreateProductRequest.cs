// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Mcp.Application.Products;

/// <summary>
/// The data needed to add a new product to the eShop catalog.
/// </summary>
/// <param name="Sku">The product's stock-keeping unit.</param>
/// <param name="Name">The product's display name.</param>
/// <param name="Description">The product's optional long-form description.</param>
/// <param name="PriceAmount">The product's initial price amount.</param>
/// <param name="PriceCurrency">The three-letter ISO 4217 currency of the initial price.</param>
/// <param name="StockQuantity">The product's initial stock quantity.</param>
/// <param name="BrandId">The identifier of the brand the product belongs to.</param>
/// <param name="CategoryId">The identifier of the category the product belongs to.</param>
public sealed record CreateProductRequest(
    string? Sku,
    string? Name,
    string? Description,
    decimal PriceAmount,
    string? PriceCurrency,
    int StockQuantity,
    Guid BrandId,
    Guid CategoryId);
