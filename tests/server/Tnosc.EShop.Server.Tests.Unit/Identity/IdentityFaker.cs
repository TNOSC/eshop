// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Bogus;

namespace Tnosc.EShop.Server.Tests.Unit.Identity;

/// <summary>
/// Domain-shaped random data for Identity tests, generated through a fixed-seed <see cref="Faker"/> so
/// a failing test's inputs are reproducible from the seed alone rather than from a captured value.
/// </summary>
internal static class IdentityFaker
{
    private const int Seed = 20260812;

    private static readonly string[] CountryCodes = ["TN", "FR", "DE", "GB", "US"];

    /// <summary>
    /// Creates a fresh, fixed-seed <see cref="Faker"/> for a single test to draw random data from.
    /// </summary>
    public static Faker New() => new() { Random = new Randomizer(Seed) };

    /// <summary>
    /// A Keycloak-shaped subject identifier — a UUID string, as Keycloak happens to issue today.
    /// </summary>
    public static string ExternalUserId(this Faker faker) => faker.Random.Guid().ToString();

    /// <summary>
    /// A lowercase email address — always valid per <c>Email.Create</c>.
    /// </summary>
    public static string Email(this Faker faker) =>
        $"{faker.Random.String2(length: 8, chars: "abcdefghijklmnopqrstuvwxyz")}@{faker.Internet.DomainName()}";

    public static string FirstName(this Faker faker) => faker.Name.FirstName();

    public static string LastName(this Faker faker) => faker.Name.LastName();

    /// <summary>
    /// An E.164-shaped number — always valid per <c>PhoneNumber.Create</c>.
    /// </summary>
    public static string PhoneNumber(this Faker faker) => $"+{faker.Random.Long(min: 1000000000, max: 999999999999)}";

    public static string Street(this Faker faker) => faker.Address.StreetAddress();

    public static string City(this Faker faker) => faker.Address.City();

    public static string PostalCode(this Faker faker) => faker.Random.Number(min: 10000, max: 99999).ToString(provider: System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// A two-letter ISO 3166-1 alpha-2 country code — always valid per <c>Address.Create</c>.
    /// </summary>
    public static string Country(this Faker faker) => faker.PickRandom(items: CountryCodes);
}
