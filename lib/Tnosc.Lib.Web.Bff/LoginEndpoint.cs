// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Tnosc.Lib.Web.Bff;

/// <summary>Starts the OIDC code-flow challenge against the identity provider.</summary>
public static class LoginEndpoint
{
    /// <summary>Maps <see cref="BffRoutes.Login"/>.</summary>
    /// <param name="app">The application to map the route on.</param>
    public static void MapLogin(WebApplication app) =>
        app.MapGet(pattern: BffRoutes.Login, handler: Handle).AllowAnonymous();

    private static ChallengeHttpResult Handle(string? returnUrl) =>
        TypedResults.Challenge(
            properties: new AuthenticationProperties { RedirectUri = GetSafeReturnUrl(returnUrl: returnUrl) },
            authenticationSchemes: [OpenIdConnectDefaults.AuthenticationScheme]);

    // Rejects anything but a same-origin relative path, so /bff/login?returnUrl=https://evil.example
    // cannot become an open redirect. "//host" (protocol-relative) and "/\host" (some browsers treat
    // backslash as a path separator) are rejected too, not just an absolute scheme.
    private static string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrEmpty(value: returnUrl)
            || !returnUrl.StartsWith(value: '/')
            || returnUrl.StartsWith(value: "//", comparisonType: StringComparison.Ordinal)
            || returnUrl.StartsWith(value: "/\\", comparisonType: StringComparison.Ordinal))
        {
            return "/";
        }

        return returnUrl;
    }
}
