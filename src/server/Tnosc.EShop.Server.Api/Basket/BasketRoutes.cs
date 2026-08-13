// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Api.Basket;

/// <summary>
/// The route templates and OpenAPI tag shared by the Basket endpoints, so a path is spelled once.
/// </summary>
/// <remarks>
/// Every route resolves the caller's own basket from their token rather than from a route value — the
/// same "no ownership check exists because there is nothing to check" shape as Identity's <c>me</c>
/// routes. See <c>.claude/rules/authorization.md</c>.
/// </remarks>
internal static class BasketRoutes
{
    /// <summary>
    /// The OpenAPI tag every Basket endpoint is grouped under.
    /// </summary>
    public const string Tag = "Basket";

    /// <summary>
    /// The caller's own basket.
    /// </summary>
    public const string CurrentBasket = "/api/basket";

    /// <summary>
    /// The caller's own basket's line collection.
    /// </summary>
    public const string CurrentBasketItems = $"{CurrentBasket}/items";

    /// <summary>
    /// A single line of the caller's own basket.
    /// </summary>
    public const string CurrentBasketItemById = $"{CurrentBasketItems}/{{itemId:guid}}";
}
