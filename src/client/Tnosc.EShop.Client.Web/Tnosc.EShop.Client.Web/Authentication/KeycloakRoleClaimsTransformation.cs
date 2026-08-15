// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Tnosc.EShop.Client.Web.Authentication;

/// <summary>
/// Reads Keycloak's <c>realm_access.roles</c> claim out of the raw access token and adds a
/// <see cref="ClaimTypes.Role"/> claim per role to the OIDC principal.
/// </summary>
/// <remarks>
/// This is a deliberate duplicate of
/// <c>Tnosc.EShop.Server.Host.Authentication.KeycloakClaimsTransformation</c> — the web project cannot
/// reference <c>Server.Host</c>, and a shared project would couple this client to the server's
/// composition root. Keycloak's built-in <c>roles</c> client scope puts <c>realm_access.roles</c> in
/// the <b>access token</b> only; the ID token carries no roles at all unless the realm's
/// <c>oidc-usermodel-realm-role-mapper</c> protocol mapper is present (see
/// <c>aspire/Tnosc.EShop.AppHost/Realms/eshop-realm.json</c>). Reading the access token here is what
/// makes role-based authorization work even against an unmodified realm.
/// </remarks>
internal static class KeycloakRoleClaimsTransformation
{
    private const string RealmAccessClaimType = "realm_access";

    /// <summary>Adds a role claim per Keycloak realm role found in the validated access token.</summary>
    /// <param name="context">The token-validated context supplied by the OIDC handler.</param>
    /// <returns>A completed task.</returns>
    public static Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        string? accessToken = context.TokenEndpointResponse?.AccessToken;
        if (accessToken is null || context.Principal?.Identity is not ClaimsIdentity identity)
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

        foreach (JsonElement role in roles.EnumerateArray())
        {
            if (role.GetString() is { Length: > 0 } roleValue)
            {
                identity.AddClaim(claim: new Claim(type: ClaimTypes.Role, value: roleValue));
            }
        }

        return Task.CompletedTask;
    }
}
