// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Auth.Authorization;

/// <summary>
/// The inert map from a coarse Keycloak realm role to the fine-grained permissions it grants, used to
/// expand a validated access token's roles into permission claims before they are persisted for the
/// WASM handoff.
/// </summary>
/// <remarks>
/// A deliberate duplicate of <c>Tnosc.EShop.Server.Shared.Authorization.RolePermissions</c> — see
/// <see cref="Roles"/>. Must stay in step with the server's map by hand: if a role here grants a
/// permission the API does not also grant that role, an admin page would show UI for an action the
/// API then rejects with a 403.
/// </remarks>
public static class RolePermissions
{
    private static readonly FrozenDictionary<string, ImmutableArray<string>> Map =
        new Dictionary<string, ImmutableArray<string>>(comparer: StringComparer.Ordinal)
        {
            [Roles.Admin] =
            [
                Permissions.Catalog.Write,
                Permissions.Identity.Write,
            ],
        }.ToFrozenDictionary(comparer: StringComparer.Ordinal);

    /// <summary>
    /// Returns the permissions granted by <paramref name="role"/>.
    /// </summary>
    /// <param name="role">A realm role name taken from the token's <c>realm_access.roles</c> claim.</param>
    /// <returns>
    /// The permissions the role grants, or an empty array when the role is not one this project knows
    /// about — the safe direction to fail in, since a realm may carry roles this UI has no opinion on.
    /// </returns>
    public static ImmutableArray<string> For(string role) =>
        Map.TryGetValue(key: role, value: out ImmutableArray<string> permissions) ? permissions : [];
}
