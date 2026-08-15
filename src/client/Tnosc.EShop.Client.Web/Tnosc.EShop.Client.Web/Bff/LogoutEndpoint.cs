// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Tnosc.EShop.Client.Web.Bff;

/// <summary>Signs out of both the cookie session and Keycloak.</summary>
internal static class LogoutEndpoint
{
    /// <summary>
    /// Maps <see cref="BffRoutes.Logout"/>. Deliberately <c>POST</c>, with antiforgery left on — this
    /// is the one BFF endpoint a cross-site <c>&lt;img src="..."&gt;</c> must not be able to trigger.
    /// <c>LoginDisplay.razor</c> renders it as a real form post with <c>&lt;AntiforgeryToken /&gt;</c>.
    /// </summary>
    /// <param name="app">The application to map the route on.</param>
    public static void MapLogout(WebApplication app) =>
        app.MapPost(pattern: BffRoutes.Logout, handler: Handle);

    private static SignOutHttpResult Handle() =>
        TypedResults.SignOut(
            properties: new AuthenticationProperties { RedirectUri = "/" },
            authenticationSchemes:
            [
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme,
            ]);
}
