// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Auth.Authorization;

namespace Tnosc.EShop.Client.Web.Authentication;

/// <summary>
/// Reads Keycloak's <c>realm_access.roles</c> claim out of the raw access token and adds a
/// <see cref="ClaimTypes.Role"/> claim per role, plus a <see cref="PermissionRequirement.PermissionClaimType"/>
/// claim per permission that role grants (via <see cref="RolePermissions"/>), to the OIDC principal —
/// and mirrors the ID token's <c>sub</c> claim onto <see cref="ClaimTypes.NameIdentifier"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is a deliberate duplicate of
/// <c>Tnosc.EShop.Server.Host.Authentication.KeycloakClaimsTransformation</c> — the web project cannot
/// reference <c>Server.Host</c>, and a shared project would couple this client to the server's
/// composition root. Keycloak's built-in <c>roles</c> client scope puts <c>realm_access.roles</c> in
/// the <b>access token</b> only; the ID token carries no roles at all unless the realm's
/// <c>oidc-usermodel-realm-role-mapper</c> protocol mapper is present (see
/// <c>aspire/Tnosc.EShop.AppHost/Realms/eshop-realm.json</c>). Reading the access token here is what
/// makes role-based authorization work even against an unmodified realm.
/// </para>
/// <para>
/// <see cref="WebAuthenticationExtensions.AddEShopBffAuthentication"/> sets <c>MapInboundClaims =
/// false</c>, so the principal keeps the token's raw <c>sub</c> claim type rather than
/// <see cref="ClaimTypes.NameIdentifier"/>. <c>UserInfoEndpoint</c> and
/// <c>PersistingRevalidatingAuthenticationStateProvider</c> both read the caller's id via
/// <see cref="ClaimTypes.NameIdentifier"/>, so without this the id comes back empty — silently, since
/// neither of those call sites treats a missing id as an error.
/// </remarks>
internal static class KeycloakRoleClaimsTransformation
{
    private const string RealmAccessClaimType = "realm_access";
    private const string SubjectClaimType = "sub";

    /// <summary>
    /// Adds a role claim per Keycloak realm role found in the validated access token, a permission
    /// claim per permission those roles grant, and a <see cref="ClaimTypes.NameIdentifier"/> claim
    /// mirroring the ID token's <c>sub</c>.
    /// </summary>
    /// <param name="context">The token-validated context supplied by the OIDC handler.</param>
    /// <returns>A completed task.</returns>
    public static Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
        {
            return Task.CompletedTask;
        }

        if (identity.FindFirst(type: ClaimTypes.NameIdentifier) is null
            && identity.FindFirst(type: SubjectClaimType) is { Value.Length: > 0 } subjectClaim)
        {
            identity.AddClaim(claim: new Claim(type: ClaimTypes.NameIdentifier, value: subjectClaim.Value));
        }

        string? accessToken = context.TokenEndpointResponse?.AccessToken;
        if (accessToken is null)
        {
            return Task.CompletedTask;
        }

        JsonWebToken token = new(jwtEncodedString: accessToken);
        if (!token.TryGetPayloadValue(key: RealmAccessClaimType, value: out JsonElement realmAccess))
        {
            return Task.CompletedTask;
        }

        if (!realmAccess.TryGetProperty(propertyName: "roles", value: out JsonElement roles))
        {
            return Task.CompletedTask;
        }

        HashSet<string> grantedPermissions = new(comparer: StringComparer.Ordinal);

        foreach (JsonElement role in roles.EnumerateArray())
        {
            if (role.GetString() is not { Length: > 0 } roleValue)
            {
                continue;
            }

            identity.AddClaim(claim: new Claim(type: ClaimTypes.Role, value: roleValue));

            foreach (string permission in RolePermissions.For(role: roleValue))
            {
                if (grantedPermissions.Add(item: permission))
                {
                    identity.AddClaim(claim: new Claim(
                        type: PermissionRequirement.PermissionClaimType,
                        value: permission));
                }
            }
        }

        return Task.CompletedTask;
    }
}
