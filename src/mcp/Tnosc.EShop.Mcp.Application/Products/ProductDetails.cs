// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Mcp.Application.Products;

/// <summary>
/// A single product's full details, as returned by the eShop catalog's get-by-id endpoint — the
/// fields <see cref="Product"/> leaves out of a search result.
/// </summary>
/// <param name="Id">The product's identifier.</param>
/// <param name="Sku">The product's stock-keeping unit.</param>
/// <param name="Name">The product's display name.</param>
/// <param name="Description">The product's optional long-form description.</param>
/// <param name="PriceAmount">The product's current price amount.</param>
/// <param name="PriceCurrency">The product's current price currency.</param>
/// <param name="StockQuantity">The number of units currently on hand.</param>
/// <param name="BrandId">The identifier of the product's brand.</param>
/// <param name="BrandName">The name of the product's brand.</param>
/// <param name="CategoryId">The identifier of the product's category.</param>
/// <param name="IsDiscontinued">Whether the product has been withdrawn from sale.</param>
/// <param name="ImageUrl">The URL of the product's image, if one has been set.</param>
#pragma warning disable CA1054, CA1056 // ImageUrl is a flat wire-format string like every other field here, never System.Uri.
public sealed record ProductDetails(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal PriceAmount,
    string PriceCurrency,
    int StockQuantity,
    Guid BrandId,
    string BrandName,
    Guid CategoryId,
    bool IsDiscontinued,
    string? ImageUrl);
#pragma warning restore CA1054, CA1056
