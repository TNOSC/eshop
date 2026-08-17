// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.Lib.Web.Api;

/// <summary>
/// Attaches <c>X-Requested-With: XMLHttpRequest</c> to every outbound request made by a Blazor
/// WebAssembly typed API client, so no call site has to remember it. This is the same-origin proof a
/// BFF proxy's same-origin check (see <c>Tnosc.Lib.Web.Bff</c>) verifies in place of an antiforgery
/// token, which a WASM <c>fetch</c> cannot carry.
/// </summary>
public sealed class RequestedWithHandler : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation(name: "X-Requested-With", value: "XMLHttpRequest");
        return base.SendAsync(request: request, cancellationToken: cancellationToken);
    }
}
