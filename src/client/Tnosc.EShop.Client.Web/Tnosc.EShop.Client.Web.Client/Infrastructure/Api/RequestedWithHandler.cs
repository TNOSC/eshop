// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>
/// Attaches <c>X-Requested-With: XMLHttpRequest</c> to every outbound request made by the WASM
/// typed API clients, so no call site has to remember it. This is the same-origin proof the BFF
/// proxy's <c>SameOriginRequirement</c> checks in place of an antiforgery token, which a WASM
/// <c>fetch</c> cannot carry.
/// </summary>
internal sealed class RequestedWithHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation(name: "X-Requested-With", value: "XMLHttpRequest");
        return base.SendAsync(request: request, cancellationToken: cancellationToken);
    }
}
