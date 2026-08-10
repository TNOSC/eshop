// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Catalog.Commands.CreateProduct;

namespace Tnosc.EShop.Server.Api.Catalog.CreateProduct;

/// <summary>
/// The HTTP body of <c>POST /api/catalog/products</c>.
/// </summary>
/// <param name="Sku">The product's stock-keeping unit.</param>
/// <param name="Name">The product's display name.</param>
/// <param name="Description">The product's optional long-form description.</param>
/// <param name="PriceAmount">The product's initial price amount.</param>
/// <param name="PriceCurrency">The three-letter ISO 4217 currency of the initial price.</param>
/// <param name="StockQuantity">The product's initial stock quantity.</param>
/// <param name="BrandId">The identifier of the brand the product belongs to.</param>
/// <param name="CategoryId">The identifier of the category the product belongs to.</param>
internal sealed record CreateProductRequest(
    string? Sku,
    string? Name,
    string? Description,
    decimal PriceAmount,
    string? PriceCurrency,
    int StockQuantity,
    Guid BrandId,
    Guid CategoryId)
{
    /// <summary>
    /// Maps this request onto the application command it carries.
    /// </summary>
    /// <returns>The equivalent <see cref="CreateProductCommand"/>.</returns>
    public CreateProductCommand ToCommand() =>
        new(Sku: Sku,
            Name: Name,
            Description: Description,
            PriceAmount: PriceAmount,
            PriceCurrency: PriceCurrency,
            StockQuantity: StockQuantity,
            BrandId: BrandId,
            CategoryId: CategoryId);
}
