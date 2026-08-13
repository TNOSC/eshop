// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Shared.Authorization;

/// <summary>
/// The coarse realm roles Keycloak issues in a token's <c>realm_access.roles</c> claim.
/// </summary>
/// <remarks>
/// <para>
/// Unlike a <c>[CacheTag]</c> value, these strings are <strong>not</strong> free to rename: each one
/// must equal a role name in <c>aspire/Tnosc.EShop.AppHost/Realms/eshop-realm.json</c>. Renaming one
/// here without renaming it there silently stops granting the permissions it maps to, because
/// <see cref="RolePermissions.For(string)"/> returns empty for a role it does not recognise.
/// </para>
/// <para>
/// Who holds a role is Keycloak's business, not this codebase's: <c>customer</c> arrives through the
/// realm's default-role composite on self-registration, and <c>admin</c> is assigned by an operator in
/// the admin console. Nothing in <c>Server.*</c> writes to Keycloak or grants a role.
/// </para>
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
