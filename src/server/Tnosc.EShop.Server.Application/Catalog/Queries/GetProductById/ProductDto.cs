// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Catalog.Queries.GetProductById;

/// <summary>
/// A single product, projected for reading.
/// </summary>
/// <param name="Id">The product's identifier.</param>
/// <param name="Sku">The product's stock-keeping unit.</param>
/// <param name="Name">The product's display name.</param>
/// <param name="Description">The product's optional long-form description.</param>
/// <param name="PriceAmount">The product's current price amount.</param>
/// <param name="PriceCurrency">The product's current price currency.</param>
/// <param name="StockQuantity">The number of units currently on hand.</param>
/// <param name="BrandId">The identifier of the product's brand.</param>
/// <param name="CategoryId">The identifier of the product's category.</param>
/// <param name="IsDiscontinued">Whether the product has been withdrawn from sale.</param>
public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal PriceAmount,
    string PriceCurrency,
    int StockQuantity,
    Guid BrandId,
    Guid CategoryId,
    bool IsDiscontinued);
