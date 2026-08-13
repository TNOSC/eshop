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

namespace Tnosc.EShop.Server.Host.Authentication;

/// <summary>
/// Turns Keycloak's <c>realm_access</c> claim into the standard role claims and the fine-grained
/// permission claims the rest of the application reads.
/// </summary>
/// <remarks>
/// <para>
/// This is the Keycloak-specific half of the authorization model and deliberately does not live in
/// <c>lib/</c>: <c>realm_access</c> is this identity provider's shape, whereas
/// <c>Tnosc.Lib.Host.Authorization</c> knows only about a permission claim. Point the host at a
/// different provider and this class is the only piece that changes.
/// </para>
/// <para>
/// <c>HttpUserContext</c> is untouched by design. It already reads
/// <see cref="ClaimTypes.NameIdentifier"/> (falling back to <c>sub</c>), <see cref="ClaimTypes.Email"/>
/// (falling back to <c>email</c>), <see cref="ClaimTypes.Role"/> and the permission claim — this
/// transformation exists precisely so those four keep working unchanged.
/// </para>
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
