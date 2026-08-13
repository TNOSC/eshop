// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Tnosc.EShop.Server.Shared.Authorization;

/// <summary>
/// The inert map from a coarse Keycloak realm role to the fine-grained permissions it grants.
/// </summary>
/// <remarks>
/// <para>
/// This is the seam that keeps the permission vocabulary out of the realm. Keycloak issues roles;
/// the Host's claims transformation expands each role through this table into <c>"permissions"</c>
/// claims, which is what an endpoint's <c>HasPermission(...)</c> policy then checks. Adding a
/// permission therefore never means editing <c>eshop-realm.json</c> or re-importing a realm.
/// </para>
/// <para>
/// Deliberately data, not behaviour — <c>Shared</c> holds constants and inert primitives only. It
/// decides nothing; it answers a lookup.
/// </para>
/// </remarks>
public static class RolePermissions
{
    private static readonly FrozenDictionary<string, ImmutableArray<string>> Map =
        new Dictionary<string, ImmutableArray<string>>(comparer: StringComparer.Ordinal)
        {
            [Roles.Admin] =
            [
                Permissions.Catalog.Read,
                Permissions.Catalog.Write,
                Permissions.Identity.Read,
                Permissions.Identity.Write,
                Permissions.Ordering.Read,
                Permissions.Ordering.Ship,
                Permissions.Payment.Read,
                Permissions.Payment.Write,
            ],
            [Roles.Customer] =
            [
                Permissions.Catalog.Read,
            ],
        }.ToFrozenDictionary(comparer: StringComparer.Ordinal);

    /// <summary>
    /// Returns the permissions granted by <paramref name="role"/>.
    /// </summary>
    /// <param name="role">A realm role name taken from the token's <c>realm_access.roles</c> claim.</param>
    /// <returns>
    /// The permissions the role grants, or an empty array when the role is not one this codebase
    /// knows. An unknown role granting nothing is the safe direction to fail in: a realm may carry
    /// roles this application has no opinion about, and a token is caller-supplied input.
    /// </returns>
    public static ImmutableArray<string> For(string role) =>
        Map.TryGetValue(key: role, value: out ImmutableArray<string> permissions) ? permissions : [];
}
