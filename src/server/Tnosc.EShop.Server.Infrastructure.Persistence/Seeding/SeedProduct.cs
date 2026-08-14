// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Seeding;

/// <summary>
/// One row of <see cref="SeedData.Products"/>, named by brand and category rather than by identifier
/// because the seeder generates those identifiers on the run that first creates them.
/// </summary>
/// <param name="Sku">The product's stock-keeping unit.</param>
/// <param name="Name">The product's display name.</param>
/// <param name="Description">The product's long-form description.</param>
/// <param name="PriceAmount">The product's price, in <see cref="SeedData.Currency"/>.</param>
/// <param name="Stock">The product's initial stock quantity.</param>
/// <param name="BrandName">The name of the seeded brand the product belongs to.</param>
/// <param name="CategoryName">The name of the seeded category the product belongs to.</param>
internal sealed record SeedProduct(
    string Sku,
    string Name,
    string Description,
    decimal PriceAmount,
    int Stock,
    string BrandName,
    string CategoryName);
