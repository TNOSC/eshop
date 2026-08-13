// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Shared.Basket;

/// <summary>
/// Cache tags shared by the Basket bounded context's <c>[CacheTag]</c> handlers, so the write
/// handlers that invalidate and the query handler that populates the cache cannot drift apart.
/// </summary>
public static class CacheTags
{
    /// <summary>
    /// Tag covering every cached Basket query — invalidated by every Basket write handler.
    /// </summary>
    public const string Basket = "basket";
}
