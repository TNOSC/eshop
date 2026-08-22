// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Tnosc.EShop.Client.Web.Bff;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.Lib.Web.Bff;

namespace Tnosc.EShop.Client.Web.Extensions;

/// <summary>Registration entry point for every BFF endpoint.</summary>
internal static class BffEndpointExtensions
{
    /// <summary>
    /// Maps the BFF endpoints. Call after <c>UseAuthentication()</c>/<c>UseAuthorization()</c> and
    /// <c>app.UseAntiforgery()</c>, and before <c>app.MapRazorComponents&lt;App&gt;()</c> — the proxy
    /// and <see cref="UserInfoEndpoint"/> read the authenticated
    /// <see cref="Microsoft.AspNetCore.Http.HttpContext"/>.
    /// </summary>
    /// <param name="app">The application to map the endpoints on.</param>
    public static void MapBffEndpoints(this WebApplication app)
    {
        LoginEndpoint.MapLogin(app: app);
        LogoutEndpoint.MapLogout(app: app);
        UserInfoEndpoint.MapUserInfo(app: app);
        BffProxy.MapProxy(
            app: app,
            downstreamClientName: ApiClientNames.Downstream,
            anonymousGetCatchAll: EShopBffRoutes.CatalogCatchAll);

        // The agent host is a second downstream, not part of the API, so it gets its own prefix and
        // its own forwarder client.
        BffProxy.MapForward(
            app: app,
            pattern: EShopBffRoutes.AgentCatchAll,
            downstreamClientName: ApiClientNames.AgentDownstream);
    }
}
