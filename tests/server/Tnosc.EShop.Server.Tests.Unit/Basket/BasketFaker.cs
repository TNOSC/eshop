// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Bogus;

namespace Tnosc.EShop.Server.Tests.Unit.Basket;

/// <summary>
/// Domain-shaped random data for Basket tests, generated through a fixed-seed <see cref="Faker"/> so a
/// failing test's inputs are reproducible from the seed alone rather than from a captured value.
/// </summary>
internal static class BasketFaker
{
    private const int Seed = 20260813;

    private static readonly string[] ValidCurrencies = ["EUR", "USD", "TND", "GBP"];

    /// <summary>
    /// Creates a fresh, fixed-seed <see cref="Faker"/> for a single test to draw random data from.
    /// </summary>
    public static Faker New() => new() { Random = new Randomizer(Seed) };

    /// <summary>
    /// A random customer identifier.
    /// </summary>
    public static Guid CustomerId(this Faker faker) => Guid.CreateVersion7();

    /// <summary>
    /// A random product identifier.
    /// </summary>
    public static Guid ProductId(this Faker faker) => Guid.CreateVersion7();

    public static string Sku(this Faker faker) =>
        $"{faker.Random.String2(length: faker.Random.Int(min: 3, max: 8), chars: "ABCDEFGHIJKLMNOPQRSTUVWXYZ")}-{faker.Random.Number(min: 1, max: 9999)}";

    public static string ProductName(this Faker faker) => faker.Commerce.ProductName();

    /// <summary>
    /// A three-letter uppercase ISO currency code — always valid per <c>Money.Create</c>.
    /// </summary>
    public static string Currency(this Faker faker) => faker.PickRandom(items: ValidCurrencies);

    public static decimal PriceAmount(this Faker faker) => Math.Round(d: faker.Random.Decimal(min: 0.01m, max: 999.99m), decimals: 2);

    /// <summary>
    /// A quantity always inside <c>Quantity.MinValue</c>..<c>Quantity.MaxValue</c>.
    /// </summary>
    public static int Quantity(this Faker faker) => faker.Random.Int(min: 1, max: 10);
}
