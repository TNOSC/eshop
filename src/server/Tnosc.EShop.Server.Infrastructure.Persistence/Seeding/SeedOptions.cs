// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Seeding;

/// <summary>
/// Controls the development-only sample data, bound from the <c>"Seed"</c> configuration section.
/// </summary>
/// <remarks>
/// The flag defaults to <see langword="false"/> on purpose: an operator has to ask for sample data,
/// and a missing or misspelled section leaves seeding off rather than on. It carries no
/// DataAnnotation for the same reason — a <see cref="bool"/> has no invalid value to validate, and
/// the safe value is the default. It is also only half the gate: <c>AddInfrastructurePersistence</c>
/// registers <see cref="DevelopmentDataSeeder"/> only in the Development environment, so setting this
/// to <see langword="true"/> in Production still seeds nothing.
/// </remarks>
internal sealed class SeedOptions
{
    /// <summary>The configuration section this class binds to.</summary>
    public const string SectionName = "Seed";

    /// <summary>
    /// Gets or sets a value indicating whether the sample catalogue and test customer are seeded on
    /// startup. Defaults to <see langword="false"/>.
    /// </summary>
    public bool Enabled { get; set; }
}
