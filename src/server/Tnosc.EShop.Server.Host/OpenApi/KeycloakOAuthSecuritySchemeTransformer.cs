// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Tnosc.EShop.Server.Host.Options;

namespace Tnosc.EShop.Server.Host.OpenApi;

/// <summary>
/// Declares an OAuth2 authorization-code security scheme against Keycloak on the generated OpenAPI
/// document, so the Scalar UI's "Authorize" button can drive a real login instead of requiring a
/// token pasted in by hand.
/// </summary>
internal sealed class KeycloakOAuthSecuritySchemeTransformer(KeycloakOptions options) : IOpenApiDocumentTransformer
{
    /// <summary>The security scheme name shared with <see cref="AuthorizedOperationTransformer"/> and the Scalar wiring in <c>Program.cs</c>.</summary>
    public const string SecuritySchemeName = "Keycloak";

    /// <inheritdoc />
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(comparer: StringComparer.Ordinal);

        string authorizationBaseUrl = $"{options.PublicAuthorityUrl}/realms/{options.Realm}/protocol/openid-connect";

        document.Components.SecuritySchemes[SecuritySchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Keycloak login — the realm's hosted authorization-code flow with PKCE.",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri(uriString: $"{authorizationBaseUrl}/auth"),
                    TokenUrl = new Uri(uriString: $"{authorizationBaseUrl}/token"),
                    Scopes = new Dictionary<string, string>(comparer: StringComparer.Ordinal)
                    {
                        ["openid"] = "OpenID Connect",
                        ["profile"] = "Profile",
                        ["email"] = "Email",
                    },
                },
            },
        };

        return Task.CompletedTask;
    }
}
