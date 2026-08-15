// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Frozen;

namespace Tnosc.EShop.Client.Web.Bff;

/// <summary>
/// Deny-lists for the headers the BFF proxy strips when forwarding a request or response. A
/// deny-list is deliberate — an allow-list would silently drop headers such as
/// <c>Idempotency-Key</c> that the proxy has no reason to know about individually. See
/// <c>plan/05-bff-proxy.md</c> for the failure mode an allow-list produces.
/// </summary>
internal static class BffProxyHeaders
{
    /// <summary>
    /// Headers never forwarded from the browser to the downstream API. <c>Cookie</c> is denied
    /// because the session cookie is the BFF's own business; <c>Authorization</c> is denied so a
    /// caller cannot smuggle a token past the proxy — the proxy sets it itself, after this list runs.
    /// </summary>
    public static readonly FrozenSet<string> RequestDenyList =
        new[]
        {
            "Host", "Cookie", "Connection", "Keep-Alive", "Proxy-Authorization",
            "TE", "Trailer", "Transfer-Encoding", "Upgrade", "Authorization",
        }.ToFrozenSet(comparer: StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Headers never copied back from the downstream API's response to the browser.
    /// <c>Transfer-Encoding</c> is denied here too, or Kestrel and <see cref="System.Net.Http.HttpClient"/>
    /// disagree about response framing.
    /// </summary>
    public static readonly FrozenSet<string> ResponseDenyList =
        new[] { "Transfer-Encoding", "Connection", "Keep-Alive", "Upgrade" }
            .ToFrozenSet(comparer: StringComparer.OrdinalIgnoreCase);
}
