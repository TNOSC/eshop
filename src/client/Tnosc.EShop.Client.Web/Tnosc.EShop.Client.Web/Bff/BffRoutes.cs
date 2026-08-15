// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Bff;

/// <summary>Route patterns owned by the BFF endpoints.</summary>
internal static class BffRoutes
{
    /// <summary>
    /// Catch-all pattern forwarding every authenticated request under <c>/bff/api/</c> to the
    /// downstream API, preserving the remainder of the path as <c>path</c>.
    /// </summary>
    public const string ApiCatchAll = "/bff/api/{**path}";

    /// <summary>
    /// Catch-all pattern for the anonymous carve-out — Catalog read endpoints only, and only for
    /// <c>GET</c>, so a signed-out visitor can still browse the storefront once WASM takes over.
    /// </summary>
    public const string CatalogCatchAll = "/bff/api/catalog/{**path}";

    /// <summary>Starts the OIDC code-flow challenge against Keycloak.</summary>
    public const string Login = "/bff/login";

    /// <summary>Signs out of both the cookie session and Keycloak.</summary>
    public const string Logout = "/bff/logout";

    /// <summary>Returns the authenticated caller's identity and roles.</summary>
    public const string UserInfo = "/bff/userinfo";
}
