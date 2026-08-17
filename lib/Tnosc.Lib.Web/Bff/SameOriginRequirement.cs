// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Tnosc.Lib.Web.Bff;

/// <summary>
/// The compensating CSRF defence for a BFF proxy route that must disable antiforgery because a WASM
/// <c>fetch</c> carries no antiforgery token. A cross-site HTML form cannot set a custom header, and
/// doing so from script triggers a CORS preflight the BFF host does not answer — so the header's
/// presence is proof the request came from the app's own origin. Pair this with <c>SameSite=Lax</c> on
/// the session cookie, which already blocks cross-site form <c>POST</c>s from carrying the cookie at
/// all; <see cref="Tnosc.Lib.Web.Api.RequestedWithHandler"/> is the WASM-side handler that sets the
/// header this checks for.
/// </summary>
public static class SameOriginRequirement
{
    private const string HeaderName = "X-Requested-With";
    private const string HeaderValue = "XMLHttpRequest";

    /// <summary>Whether <paramref name="request"/> carries the same-origin marker header.</summary>
    /// <param name="request">The inbound request to the BFF proxy.</param>
    public static bool IsSatisfied(HttpRequest request) =>
        request.Headers.TryGetValue(key: HeaderName, value: out StringValues requestedWith)
        && StringValues.Equals(requestedWith, HeaderValue);
}
