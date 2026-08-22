// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Host.Authorization;

namespace Tnosc.EShop.Mcp.Host.Authentication;

/// <summary>
/// Turns Keycloak's <c>realm_access</c> claim into the standard role claims and the fine-grained
/// permission claims <see cref="Tnosc.Lib.Host.Authorization.PermissionAuthorizationHandler"/> reads.
/// </summary>
/// <remarks>
/// A near-duplicate of <c>Tnosc.EShop.Server.Host.Authentication.KeycloakClaimsTransformation</c>: the
/// MCP host and the eShop API host are two separate ASP.NET Core applications authenticating against
/// the same realm, so each needs its own claims transformation registered against its own
/// authentication pipeline. Both share the permission vocabulary in
/// <see cref="Tnosc.EShop.Server.Shared.Authorization.RolePermissions"/>, which is exactly the
/// "two projects that cannot see each other must agree on a literal" case <c>cache-tags.md</c> and
/// <c>authorization.md</c> describe — <c>Permissions.Catalog.Write</c>, named on
/// <c>ProductsTool.CreateProductAsync</c> via <c>[Authorize]</c>, must resolve to the same string this
/// transformation grants.
/// </remarks>
public sealed class KeycloakClaimsTransformation : IClaimsTransformation
{
    private const string RealmAccessClaimType = "realm_access";
    private const string RolesPropertyName = "roles";

    /// <summary>
    /// Adds a <see cref="ClaimTypes.Role"/> claim per Keycloak realm role, then expands each role
    /// through <see cref="RolePermissions"/> into permission claims.
    /// </summary>
    /// <param name="principal">The principal built from the validated access token.</param>
    /// <returns>The same principal, enriched with role and permission claims.</returns>
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(argument: principal);

        if (principal.Identity is not ClaimsIdentity identity)
        {
            return Task.FromResult(result: principal);
        }

        foreach (string role in ReadRealmRoles(principal: principal))
        {
            AddClaimOnce(identity: identity, type: ClaimTypes.Role, value: role);

            foreach (string permission in RolePermissions.For(role: role))
            {
                AddClaimOnce(
                    identity: identity,
                    type: PermissionRequirement.PermissionClaimType,
                    value: permission);
            }
        }

        return Task.FromResult(result: principal);
    }

    // IClaimsTransformation can run more than once for a single request — most visibly when something
    // re-authenticates the request — so every claim is added at most once. Without this guard a second
    // run would duplicate every role and permission claim on the identity.
    private static void AddClaimOnce(ClaimsIdentity identity, string type, string value)
    {
        if (identity.HasClaim(type: type, value: value))
        {
            return;
        }

        identity.AddClaim(claim: new Claim(type: type, value: value));
    }

    private static ImmutableArray<string> ReadRealmRoles(ClaimsPrincipal principal)
    {
        string? realmAccess = principal.FindFirst(type: RealmAccessClaimType)?.Value;

        if (string.IsNullOrWhiteSpace(value: realmAccess))
        {
            return [];
        }

        // The claim arrives as a raw JSON string, not a structured claim. A malformed value is treated
        // as "no roles" rather than an exception: the token is caller-supplied input, and failing the
        // whole request with a 500 over it would turn a bad token into a server error.
        try
        {
            using var document = JsonDocument.Parse(json: realmAccess);

            if (!document.RootElement.TryGetProperty(propertyName: RolesPropertyName, value: out JsonElement roles) ||
                roles.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            List<string> parsed = [];

            foreach (JsonElement role in roles.EnumerateArray())
            {
                if (role.ValueKind == JsonValueKind.String && role.GetString() is { Length: > 0 } value)
                {
                    parsed.Add(item: value);
                }
            }

            return [.. parsed];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
