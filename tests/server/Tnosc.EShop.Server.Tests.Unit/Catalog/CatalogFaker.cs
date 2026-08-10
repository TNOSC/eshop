// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Bogus;

namespace Tnosc.EShop.Server.Tests.Unit.Catalog;

/// <summary>
/// Domain-shaped random data for Catalog tests, generated through a fixed-seed <see cref="Faker"/> so
/// a failing test's inputs are reproducible from the seed alone rather than from a captured value.
/// </summary>
internal static class CatalogFaker
{
    private const int Seed = 20260810;

    private static readonly string[] ValidCurrencies = ["EUR", "USD", "TND", "GBP"];

    /// <summary>
    /// Creates a fresh, fixed-seed <see cref="Faker"/> for a single test to draw random data from.
    /// </summary>
    public static Faker New() => new() { Random = new Randomizer(Seed) };

    /// <summary>
    /// A SKU-shaped string: uppercase letters, a dash, and digits — always valid per <c>Sku.Create</c>.
    /// </summary>
    public static string Sku(this Faker faker) =>
        $"{faker.Random.String2(length: faker.Random.Int(min: 3, max: 8), chars: "ABCDEFGHIJKLMNOPQRSTUVWXYZ")}-{faker.Random.Number(min: 1, max: 9999)}";

    public static string ProductName(this Faker faker) => faker.Commerce.ProductName();

    public static string Description(this Faker faker) => faker.Lorem.Sentence(wordCount: 10);

    /// <summary>
    /// A three-letter uppercase ISO currency code — always valid per <c>Money.Create</c>.
    /// </summary>
    public static string Currency(this Faker faker) => faker.PickRandom(items: ValidCurrencies);

    public static decimal PriceAmount(this Faker faker) => Math.Round(d: faker.Random.Decimal(min: 0.01m, max: 999.99m), decimals: 2);

    public static int StockQuantity(this Faker faker) => faker.Random.Int(min: 0, max: 500);

    public static string BrandName(this Faker faker) => faker.Company.CompanyName();

    public static string CategoryName(this Faker faker) => faker.Commerce.Categories(num: 1)[0];
}
