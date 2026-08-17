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
}
