// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Tnosc.EShop.Client.Web.Authentication;

/// <summary>
/// Attaches the authenticated caller's bearer token to outbound requests made by the server host's
/// own typed API clients — not the BFF proxy, which sets the header itself. Registered only on those
/// typed clients via the <c>configure</c> callback of <c>AddEShopApiClients</c>, never through
/// <c>ConfigureHttpClientDefaults</c>, so it never touches an unrelated <see cref="HttpClient"/>.
/// </summary>
internal sealed class ServerAccessTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpContext? context = httpContextAccessor.HttpContext;
        if (context?.User.Identity?.IsAuthenticated == true)
        {
            string? accessToken = await context.GetTokenAsync(tokenName: "access_token");
            if (accessToken is not null)
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(scheme: "Bearer", parameter: accessToken);
            }
        }

        return await base.SendAsync(request: request, cancellationToken: cancellationToken);
    }
}
