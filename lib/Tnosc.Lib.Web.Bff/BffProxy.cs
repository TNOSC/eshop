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
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Tnosc.Lib.Web.Bff;

/// <summary>
/// Forwards every <c>/bff/api/{**path}</c> request to the downstream API, attaching the caller's
/// bearer token server-side. Hand-written rather than YARP so the forwarded request goes through the
/// named <see cref="HttpClient"/> the host itself registers — which can carry
/// <c>AddStandardResilienceHandler()</c> and <c>AddServiceDiscovery()</c> from
/// <c>AddServiceDefaults()</c> the way the eShop host's does.
/// </summary>
public static class BffProxy
{
    /// <summary>Maps the catch-all forwarding routes: authenticated, plus an optional anonymous carve-out.</summary>
    /// <param name="app">The application to map the routes on.</param>
    /// <param name="downstreamClientName">
    /// The name of the <see cref="IHttpClientFactory"/> client the proxy forwards through — registered
    /// by the host itself, e.g. via <c>services.AddHttpClient(name: ...)</c>.
    /// </param>
    /// <param name="anonymousGetCatchAll">
    /// An optional route pattern, more specific than <see cref="BffRoutes.ApiCatchAll"/>, forwarded
    /// anonymously for <c>GET</c> only — e.g. a storefront's public read endpoints, so a signed-out
    /// visitor can still browse before authenticating. A more specific literal segment beats the
    /// authenticated catch-all's parameter by ASP.NET Core's route precedence, so this wins for
    /// matching <c>GET</c>s; everything else still falls through to the authenticated route. Omit to
    /// forward every request under <c>/bff/api/</c> only when authenticated.
    /// </param>
    public static void MapProxy(WebApplication app, string downstreamClientName, string? anonymousGetCatchAll = null)
    {
        ArgumentNullException.ThrowIfNull(argument: app);
        ArgumentException.ThrowIfNullOrEmpty(argument: downstreamClientName);

        // Authenticated: everything under /bff/api.
        MapForward(app: app, pattern: BffRoutes.ApiCatchAll, downstreamClientName: downstreamClientName);

        if (anonymousGetCatchAll is not null)
        {
            app.MapGet(pattern: anonymousGetCatchAll, handler: (HttpContext context, IHttpClientFactory factory, CancellationToken cancellationToken) =>
                    ForwardAsync(context: context, factory: factory, downstreamClientName: downstreamClientName, cancellationToken: cancellationToken))
                .AllowAnonymous();
        }
    }

    /// <summary>
    /// Maps a single authenticated catch-all pattern under <c>/bff/</c> onto one downstream service.
    /// </summary>
    /// <param name="app">The application to map the route on.</param>
    /// <param name="pattern">
    /// The route pattern to forward, which must start with <c>/bff/</c> — that prefix is stripped and
    /// the remainder becomes the downstream path, so <c>/bff/agents/{**path}</c> reaches the
    /// downstream as <c>agents/…</c>.
    /// </param>
    /// <param name="downstreamClientName">
    /// The name of the <see cref="IHttpClientFactory"/> client this pattern forwards through.
    /// </param>
    /// <remarks>
    /// A host with more than one downstream calls this once per downstream, each with its own client
    /// name and prefix. <c>DisableAntiforgery</c> is safe only because <see cref="SameOriginRequirement"/>
    /// provides a compensating CSRF defence inside the forwarding handler.
    /// </remarks>
    public static void MapForward(WebApplication app, string pattern, string downstreamClientName)
    {
        ArgumentNullException.ThrowIfNull(argument: app);
        ArgumentException.ThrowIfNullOrEmpty(argument: pattern);
        ArgumentException.ThrowIfNullOrEmpty(argument: downstreamClientName);

        app.Map(pattern: pattern, handler: (HttpContext context, IHttpClientFactory factory, CancellationToken cancellationToken) =>
                ForwardAsync(context: context, factory: factory, downstreamClientName: downstreamClientName, cancellationToken: cancellationToken))
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    private static async Task ForwardAsync(
        HttpContext context,
        IHttpClientFactory factory,
        string downstreamClientName,
        CancellationToken cancellationToken)
    {
        if (!SameOriginRequirement.IsSatisfied(request: context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Guarded rather than called unconditionally: an anonymous carve-out request has no
        // authenticated user, and GetTokenAsync throws rather than returning null in that case.
        string? accessToken = context.User.Identity?.IsAuthenticated == true
            ? await context.GetTokenAsync(tokenName: "access_token")
            : null;

        HttpClient client = factory.CreateClient(name: downstreamClientName);
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

        // Required for a downstream that streams — a server-sent-event conversation, for instance.
        // The response body feature buffers by default, which for a JSON response is invisible but for
        // an event stream means every delta is held until the downstream closes the connection, so the
        // browser sees one block at the end instead of a stream. Harmless for the buffered routes.
        context.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

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
