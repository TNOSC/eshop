// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Seeding;

/// <summary>
/// The sample catalogue and test customer <see cref="DevelopmentDataSeeder"/> writes.
/// </summary>
/// <remarks>
/// Held as data rather than as code so the seeder itself stays a single loop per aggregate, and so
/// the acceptance suite and the README can name a SKU that is guaranteed to exist. Every value here
/// still goes through a domain factory on the way in — nothing bypasses an invariant.
/// </remarks>
internal static class SeedData
{
    /// <summary>The currency every seeded price is denominated in.</summary>
    public const string Currency = "EUR";

    /// <summary>
    /// The SKU of the product the acceptance suite buys. Kept as a constant because a test that
    /// searched for "some product" would pass against an empty catalogue.
    /// </summary>
    public const string FeaturedSku = "TNOSC-LAPTOP-13";

    /// <summary>The external-identity subject of the seeded demo customer.</summary>
    /// <remarks>
    /// A fixed, synthetic id that deliberately matches no Keycloak account: realm import generates a
    /// fresh <c>sub</c> for <c>customer@eshop.local</c> on every fresh database, so a seeded profile
    /// could never be that user's. It is demo data for the customer listing, and its email is
    /// distinct from both realm users' for the same reason — sharing one would make their first
    /// <c>POST /api/identity/customers</c> fail with <c>Customer.EmailAlreadyRegistered</c>.
    /// </remarks>
    public const string DemoCustomerExternalUserId = "00000000-0000-0000-0000-0000000000d1";

    /// <summary>The email of the seeded demo customer.</summary>
    public const string DemoCustomerEmail = "demo.customer@eshop.local";

    /// <summary>The brand names to seed, in order.</summary>
    public static IReadOnlyList<string> Brands { get; } = ["Contoso", "Fabrikam", "Northwind"];

    /// <summary>The category names to seed, in order.</summary>
    public static IReadOnlyList<string> Categories { get; } = ["Laptops", "Phones", "Accessories"];

    /// <summary>The products to seed, in order.</summary>
    public static IReadOnlyList<SeedProduct> Products { get; } =
    [
        new SeedProduct(
            Sku: FeaturedSku,
            Name: "Contoso UltraBook 13",
            Description: "A 13-inch ultraportable laptop.",
            PriceAmount: 1299.00m,
            Stock: 25,
            BrandName: "Contoso",
            CategoryName: "Laptops"),
        new SeedProduct(
            Sku: "TNOSC-LAPTOP-15",
            Name: "Fabrikam WorkStation 15",
            Description: "A 15-inch developer workstation.",
            PriceAmount: 1899.50m,
            Stock: 12,
            BrandName: "Fabrikam",
            CategoryName: "Laptops"),
        new SeedProduct(
            Sku: "TNOSC-PHONE-X",
            Name: "Contoso Phone X",
            Description: "A flagship handset.",
            PriceAmount: 899.00m,
            Stock: 40,
            BrandName: "Contoso",
            CategoryName: "Phones"),
        new SeedProduct(
            Sku: "TNOSC-PHONE-MINI",
            Name: "Northwind Phone Mini",
            Description: "A compact handset.",
            PriceAmount: 549.00m,
            Stock: 60,
            BrandName: "Northwind",
            CategoryName: "Phones"),
        new SeedProduct(
            Sku: "TNOSC-ACC-MOUSE",
            Name: "Fabrikam Wireless Mouse",
            Description: "A low-latency wireless mouse.",
            PriceAmount: 49.90m,
            Stock: 200,
            BrandName: "Fabrikam",
            CategoryName: "Accessories"),
    ];
}
