// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

namespace Tnosc.EShop.Client.Web.Bff;

/// <summary>
/// Forwards every <c>/bff/api/{**path}</c> request to the downstream API, attaching the caller's
/// bearer token server-side. Hand-written rather than YARP so the forwarded request goes through the
/// named <see cref="HttpClient"/> that already carries <c>AddStandardResilienceHandler()</c> and
/// <c>AddServiceDiscovery()</c> from <c>AddServiceDefaults()</c> — see <c>plan/05-bff-proxy.md</c> for
/// the full reasoning.
/// </summary>
internal static class BffProxy
{
    /// <summary>Maps the catch-all forwarding routes: authenticated, plus an anonymous carve-out.</summary>
    /// <param name="app">The application to map the routes on.</param>
    public static void MapProxy(WebApplication app)
    {
        // Authenticated: everything under /bff/api. DisableAntiforgery is safe only because
        // SameOriginRequirement provides a compensating CSRF defence inside ForwardAsync.
        app.Map(pattern: BffRoutes.ApiCatchAll, handler: ForwardAsync)
            .RequireAuthorization()
            .DisableAntiforgery();

        // Carve-out: anonymous GETs against the Catalog read endpoints only, so a signed-out visitor
        // can still browse the storefront once WASM takes over. GET-only is the important half — a
        // more specific literal segment (/catalog/) beats the authenticated catch-all's parameter by
        // ASP.NET Core's route precedence, so this wins for Catalog GETs; everything else, including
        // POST /api/catalog/products, falls through to the authenticated route above.
        app.MapGet(pattern: BffRoutes.CatalogCatchAll, handler: ForwardAsync)
            .AllowAnonymous();
    }

    private static async Task ForwardAsync(
        HttpContext context,
        IHttpClientFactory factory,
        CancellationToken cancellationToken)
    {
        if (!SameOriginRequirement.IsSatisfied(request: context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Guarded rather than called unconditionally: an anonymous Catalog-read carve-out request has
        // no authenticated user, and GetTokenAsync throws rather than returning null in that case.
        string? accessToken = context.User.Identity?.IsAuthenticated == true
            ? await context.GetTokenAsync(tokenName: "access_token")
            : null;

        HttpClient client = factory.CreateClient(name: ApiClientNames.Downstream);
        using HttpRequestMessage request = new(
            method: new HttpMethod(method: context.Request.Method),
            requestUri: context.Request.Path.Value!["/bff/".Length..] + context.Request.QueryString);

        CopyRequestHeaders(source: context.Request, target: request);
        if (accessToken is not null)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(scheme: "Bearer", parameter: accessToken);
        }

        if (context.Request.ContentLength is > 0
            || context.Request.Headers.ContainsKey(key: "Transfer-Encoding"))
        {
            request.Content = new StreamContent(content: context.Request.Body);
            CopyContentHeaders(source: context.Request, target: request.Content);
        }

        using HttpResponseMessage response = await client.SendAsync(
            request: request,
            completionOption: HttpCompletionOption.ResponseHeadersRead,
            cancellationToken: cancellationToken);

        context.Response.StatusCode = (int)response.StatusCode;
        CopyResponseHeaders(source: response, target: context.Response);
        await response.Content.CopyToAsync(stream: context.Response.Body, cancellationToken: cancellationToken);
    }

    private static void CopyRequestHeaders(HttpRequest source, HttpRequestMessage target)
    {
        foreach (KeyValuePair<string, StringValues> header in source.Headers)
        {
            if (BffProxyHeaders.RequestDenyList.Contains(item: header.Key))
            {
                continue;
            }

            target.Headers.TryAddWithoutValidation(
                name: header.Key,
                values: (IEnumerable<string?>)header.Value);
        }
    }

    private static void CopyContentHeaders(HttpRequest source, HttpContent target)
    {
        foreach (KeyValuePair<string, StringValues> header in source.Headers)
        {
            if (!header.Key.StartsWith(value: "Content-", comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            target.Headers.TryAddWithoutValidation(
                name: header.Key,
                values: (IEnumerable<string?>)header.Value);
        }
    }

    private static void CopyResponseHeaders(HttpResponseMessage source, HttpResponse target)
    {
        CopyResponseHeaderCollection(source: source.Headers, target: target);
        CopyResponseHeaderCollection(source: source.Content.Headers, target: target);
    }

    private static void CopyResponseHeaderCollection(HttpHeaders source, HttpResponse target)
    {
        foreach (KeyValuePair<string, IEnumerable<string>> header in source)
        {
            if (BffProxyHeaders.ResponseDenyList.Contains(item: header.Key))
            {
                continue;
            }

            target.Headers[header.Key] = new StringValues(values: [.. header.Value]);
        }
    }
}
