// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Mcp.Application.Products;

/// <summary>
/// A product as returned by the eShop catalog's search endpoint.
/// </summary>
/// <param name="Id">The product's identifier.</param>
/// <param name="Sku">The product's stock-keeping unit.</param>
/// <param name="Name">The product's display name.</param>
/// <param name="PriceAmount">The product's current price amount.</param>
/// <param name="PriceCurrency">The product's current price currency.</param>
/// <param name="StockQuantity">The number of units currently on hand.</param>
/// <param name="BrandName">The name of the product's brand.</param>
/// <param name="CategoryName">The name of the product's category.</param>
public sealed record Product(
    Guid Id,
    string Sku,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int StockQuantity,
    string BrandName,
    string CategoryName);
