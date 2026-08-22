// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Tnosc.EShop.Mcp.Host.Authentication;

/// <summary>
/// Forwards the bearer token an MCP client presented on the inbound request onto every outbound call
/// the eShop API typed client makes, so the API sees the same caller identity the MCP server was
/// authenticated as.
/// </summary>
/// <remarks>
/// This works because the <c>mcp:tools</c> client scope's audience mapper (see
/// <c>eshop-realm.json</c>) stamps both <c>mcp-api</c> and <c>eshop-api</c> onto a token minted for
/// the MCP server — so the same token the caller authenticated to this host with is already valid
/// for the eShop API's own <c>aud=eshop-api</c> check. Attached to the
/// <see cref="Tnosc.EShop.Mcp.Application.Ports.IEShopClient"/> <c>HttpClient</c> in
/// <c>Program.cs</c> via <c>AddHttpMessageHandler&lt;TokenForwarder&gt;()</c>, never through
/// <c>ConfigureHttpClientDefaults</c>.
/// </remarks>
/// <param name="httpContextAccessor">
/// Resolves the current request's <see cref="HttpContext"/> so the incoming <c>Authorization</c>
/// header can be read; registered by <see cref="Microsoft.Extensions.DependencyInjection.HttpServiceCollectionExtensions.AddHttpContextAccessor"/>.
/// </param>
internal sealed class TokenForwarder(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? authorizationHeader = httpContextAccessor.HttpContext?.Request.Headers[HeaderNames.Authorization];

        if (!string.IsNullOrEmpty(value: authorizationHeader) &&
            AuthenticationHeaderValue.TryParse(input: authorizationHeader, parsedValue: out AuthenticationHeaderValue? parsedHeader))
        {
            request.Headers.Authorization = parsedHeader;
        }

        return base.SendAsync(request: request, cancellationToken: cancellationToken);
    }
}
