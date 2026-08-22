// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Bff;

/// <summary>
/// eShop's own addition to the generic BFF route set in <c>Tnosc.Lib.Web.Bff.BffRoutes</c> — the
/// anonymous Catalog-read carve-out is a business decision, not something a generic proxy should know.
/// </summary>
internal static class EShopBffRoutes
{
    /// <summary>
    /// Catch-all pattern for the anonymous carve-out — Catalog read endpoints only, and only for
    /// <c>GET</c>, so a signed-out visitor can still browse the storefront once WASM takes over.
    /// </summary>
    public const string CatalogCatchAll = "/bff/api/catalog/{**path}";

    /// <summary>
    /// Catch-all pattern forwarding the storefront's assistant conversation to the agent host — a
    /// second downstream alongside the API, so it needs its own prefix rather than sitting under
    /// <c>/bff/api/</c>.
    /// </summary>
    /// <remarks>
    /// Authenticated only, with no anonymous carve-out: the AG-UI endpoint isolates conversations by
    /// the caller's Keycloak subject and forwards that same token on to the MCP tools, neither of
    /// which means anything for an anonymous caller.
    /// </remarks>
    public const string AgentCatchAll = "/bff/agents/{**path}";
}
