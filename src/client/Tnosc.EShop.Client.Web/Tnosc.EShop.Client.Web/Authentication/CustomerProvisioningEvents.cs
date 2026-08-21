// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.EShop.Client.Web.Contracts.Routes;

namespace Tnosc.EShop.Client.Web.Authentication;

/// <summary>
/// Provisions the caller's local <c>Customer</c> profile right after a successful Keycloak login, by
/// calling <c>POST /api/identity/customers</c> with the freshly issued access token — the call
/// <c>ProvisionCustomerEndpoint</c>'s own contract says a client makes after every login, so the
/// customer profile exists before any page tries to read it via <c>GET /api/identity/customers/me</c>.
/// </summary>
/// <remarks>
/// Wired onto <c>OpenIdConnectOptions.Events.OnTokenValidated</c>, which fires only on an actual
/// sign-in — not on the silent token renewal <see cref="CookieRefreshEvents"/> performs, so this never
/// runs more often than a login happens. Uses the plain <see cref="ApiClientNames.Downstream"/>
/// client rather than the typed <c>IIdentityApi</c>: at this point in the OIDC handshake the cookie
/// has not been signed in yet, so <see cref="ServerAccessTokenHandler"/> (which reads the token off
/// the authenticated <c>HttpContext</c>) would find nothing — the token has to come from
/// <see cref="TokenValidatedContext.TokenEndpointResponse"/> directly instead.
/// </remarks>
internal static class CustomerProvisioningEvents
{
    /// <summary>
    /// Provisions the caller's customer profile from the token just validated. A failure is logged and
    /// swallowed — losing this call must never fail the login itself, and the endpoint is safe to call
    /// again on the caller's next sign-in.
    /// </summary>
    /// <param name="context">The token-validated context supplied by the OIDC handler.</param>
    public static async Task ProvisionAsync(TokenValidatedContext context)
    {
        string? accessToken = context.TokenEndpointResponse?.AccessToken;
        if (accessToken is null || context.Principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        ProvisionCustomerRequest request = new(
            FirstName: identity.FindFirst(type: "given_name")?.Value,
            LastName: identity.FindFirst(type: "family_name")?.Value,
            PhoneNumber: null);

        IHttpClientFactory httpClientFactory =
            context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        ILogger logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(categoryName: typeof(CustomerProvisioningEvents).FullName!);

        using HttpClient client = httpClientFactory.CreateClient(name: ApiClientNames.Downstream);
        using HttpRequestMessage httpRequest = new(method: HttpMethod.Post, requestUri: ApiRoutes.Identity.Customers)
        {
            Content = JsonContent.Create(inputValue: request),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(scheme: "Bearer", parameter: accessToken);

        try
        {
            using HttpResponseMessage response = await client.SendAsync(
                request: httpRequest,
                cancellationToken: context.HttpContext.RequestAborted);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    message: "Provisioning the caller's customer profile failed with status {StatusCode}.",
                    response.StatusCode);
            }
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception: exception, message: "Provisioning the caller's customer profile failed.");
        }
    }
}
