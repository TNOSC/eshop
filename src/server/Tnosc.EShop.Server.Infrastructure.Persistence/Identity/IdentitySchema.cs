// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Identity;

/// <summary>
/// Names the Postgres objects owned by the Identity bounded context. One schema per context keeps the
/// contexts separable inside the single database the outbox forces them to share.
/// </summary>
/// <remarks>
/// Keycloak's own ~90 tables are not here and never will be: it owns a separate <c>keycloakdb</c>
/// database on the same server, so nothing it creates can collide with a migration, a Respawn reset
/// or the outbox.
/// </remarks>
internal static class IdentitySchema
{
    /// <summary>
    /// The Postgres schema every Identity table lives in.
    /// </summary>
    public const string Name = "identity";

    /// <summary>
    /// The name of the customers table.
    /// </summary>
    public const string CustomersTable = "customers";

    /// <summary>
    /// The name of the customer addresses table.
    /// </summary>
    public const string AddressesTable = "customer_addresses";
}
