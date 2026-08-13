// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure;

/// <summary>
/// Mints access tokens for the API tests, signed with a symmetric key
/// <see cref="EShopApiFactory"/> also configures the host to trust.
/// </summary>
/// <remarks>
/// The token carries a <c>realm_access</c> claim in Keycloak's own JSON shape, so the real
/// <c>KeycloakClaimsTransformation</c> runs during the test and the permissions being asserted are the
/// ones production would derive. A token pre-loaded with <c>permissions</c> claims would restate the
/// fixture rather than prove anything.
/// </remarks>
internal static class TestTokenIssuer
{
    /// <summary>
    /// The symmetric signing key shared with the test host. Long enough for HMAC-SHA256.
    /// </summary>
    public const string SigningKey = "tnosc-eshop-integration-test-signing-key-please-do-not-reuse";

    /// <summary>
    /// The issuer the test host is configured to accept.
    /// </summary>
    public const string Issuer = "https://tnosc.test/realms/eshop";

    /// <summary>
    /// The audience the test host is configured to accept.
    /// </summary>
    public const string Audience = "eshop-api";

    /// <summary>
    /// Mints a signed access token for a caller holding the supplied realm roles.
    /// </summary>
    /// <param name="subject">The value of the <c>sub</c> claim.</param>
    /// <param name="email">The value of the <c>email</c> claim.</param>
    /// <param name="realmRoles">The realm roles to place inside the <c>realm_access</c> claim.</param>
    /// <returns>The encoded JWT.</returns>
    public static string Issue(string subject, string email, params string[] realmRoles)
    {
        string realmAccess = $"{{\"roles\":[{string.Join(separator: ",", values: realmRoles.Select(selector: role => $"\"{role}\""))}]}}";

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(value: 30),
            Claims = new Dictionary<string, object>(comparer: StringComparer.Ordinal)
            {
                [JwtRegisteredClaimNames.Sub] = subject,
                [JwtRegisteredClaimNames.Email] = email,
                [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),

                // Emitted as a raw JSON string, exactly as Keycloak sends it — the transformation
                // parses it with System.Text.Json rather than reading a structured claim.
                ["realm_access"] = realmAccess,
            },
            SigningCredentials = new SigningCredentials(
                key: new SymmetricSecurityKey(key: Encoding.UTF8.GetBytes(s: SigningKey)),
                algorithm: SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(tokenDescriptor: descriptor);
    }

    /// <summary>
    /// Mints a token whose signature the host will reject, for asserting the unauthenticated path.
    /// </summary>
    /// <returns>The encoded JWT, signed with the wrong key.</returns>
    public static string IssueWithWrongKey()
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(value: 30),
            Claims = new Dictionary<string, object>(comparer: StringComparer.Ordinal)
            {
                [JwtRegisteredClaimNames.Sub] = Guid.CreateVersion7().ToString(),
            },
            SigningCredentials = new SigningCredentials(
                key: new SymmetricSecurityKey(key: Encoding.UTF8.GetBytes(s: new string(c: 'x', count: SigningKey.Length))),
                algorithm: SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(tokenDescriptor: descriptor);
    }
}
