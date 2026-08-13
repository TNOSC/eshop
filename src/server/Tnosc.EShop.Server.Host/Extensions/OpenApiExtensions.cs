// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Scalar.AspNetCore;
using Tnosc.EShop.Server.Host.OpenApi;

namespace Tnosc.EShop.Server.Host.Extensions;

internal static class OpenApiExtensions
{
    internal static void AddOpenApiWithAuthSupport(this WebApplication webApplication)
    {
        webApplication.MapOpenApi();
        webApplication.MapScalarApiReference(configureOptions: options => options
            .WithTitle(title: "Tnosc EShop API")
            .AddPreferredSecuritySchemes(KeycloakOAuthSecuritySchemeTransformer.SecuritySchemeName)
            .AddAuthorizationCodeFlow(
                securitySchemeName: KeycloakOAuthSecuritySchemeTransformer.SecuritySchemeName,
                configureFlow: flow =>
                {
                    flow.ClientId = "eshop-web";
                    flow.Pkce = Pkce.Sha256;
                    flow.SelectedScopes = ["openid", "profile", "email"];
                }));
    }
}
