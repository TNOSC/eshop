// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Auth.Authorization;

/// <summary>
/// The coarse realm roles Keycloak issues in a token's <c>realm_access.roles</c> claim.
/// </summary>
/// <remarks>
/// A deliberate duplicate of <c>Tnosc.EShop.Server.Shared.Authorization.Roles</c> — this project
/// cannot reference <c>Server.Shared</c> without coupling the Blazor client to the server's
/// composition root, for the same reason the server host's <c>KeycloakRoleClaimsTransformation</c>
/// duplicates rather than reuses <c>Server.Host</c>'s claims transformation. Each value must still
/// equal a role name in <c>aspire/Tnosc.EShop.AppHost/Realms/eshop-realm.json</c>.
/// </remarks>
public static class Roles
{
    /// <summary>
    /// Back-office operator. Never granted by self-registration.
    /// </summary>
    public const string Admin = "admin";

    /// <summary>
    /// Storefront shopper. Granted to every new user by the realm's default-role composite.
    /// </summary>
    public const string Customer = "customer";
}
