// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>
/// The logical names the typed API clients are registered under, so their outbound requests are
/// identifiable in logging and telemetry regardless of which host resolves them.
/// </summary>
public static class ApiClientNames
{
    /// <summary>The Catalog typed client.</summary>
    public const string Catalog = "eshop-catalog";

    /// <summary>The Basket typed client.</summary>
    public const string Basket = "eshop-basket";

    /// <summary>The Ordering typed client.</summary>
    public const string Ordering = "eshop-ordering";

    /// <summary>The Identity typed client.</summary>
    public const string Identity = "eshop-identity";

    /// <summary>The shopping assistant's AG-UI conversation client, targeting the agent host.</summary>
    public const string Agent = "eshop-agent";

    /// <summary>The BFF's downstream forwarder client, targeting <c>eshop-host</c> directly.</summary>
    public const string Downstream = "eshop-downstream";

    /// <summary>
    /// The BFF's second downstream forwarder client, targeting <c>eshop-agent</c> directly.
    /// </summary>
    /// <remarks>
    /// Kept separate from <see cref="Agent"/> for the same reason <see cref="Downstream"/> is kept
    /// separate from the typed clients: the proxy sets <c>Authorization</c> itself from the incoming
    /// request, so the host's access-token handler must never be attached to this one.
    /// </remarks>
    public const string AgentDownstream = "eshop-agent-downstream";
}
