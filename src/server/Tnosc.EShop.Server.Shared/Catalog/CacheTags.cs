// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Shared.Catalog;

/// <summary>
/// Cache tags shared by the Catalog bounded context's <c>[CacheTag]</c> handlers, so the write
/// handlers that invalidate and the query handlers that populate the cache cannot drift apart.
/// </summary>
public static class CacheTags
{
    /// <summary>
    /// Tag covering every cached Catalog query — invalidated by every Catalog write handler.
    /// </summary>
    public const string Catalog = "catalog";
}
