// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Api.Ordering;

/// <summary>
/// The route templates and OpenAPI tag shared by the Ordering endpoints, so a path is spelled once.
/// </summary>
/// <remarks>
/// The collection routes address the caller's own orders, resolved from their token rather than from a
/// route value — the same shape as Identity's <c>me</c> routes and Basket's. The identifier in
/// <see cref="OrderById"/> is not an exception: the caller's identity goes into the lookup alongside
/// it, so an order that is not theirs is simply not found.
/// </remarks>
internal static class OrderingRoutes
{
    /// <summary>
    /// The OpenAPI tag every Ordering endpoint is grouped under.
    /// </summary>
    public const string Tag = "Ordering";

    /// <summary>
    /// The caller's own orders.
    /// </summary>
    public const string Orders = "/api/orders";

    /// <summary>
    /// A single order by identifier.
    /// </summary>
    public const string OrderById = $"{Orders}/{{id:guid}}";

    /// <summary>
    /// A single order's rolled-up back-office summary.
    /// </summary>
    public const string OrderSummary = $"{OrderById}/summary";

    /// <summary>
    /// Confirming a single order.
    /// </summary>
    public const string OrderConfirm = $"{OrderById}/confirm";

    /// <summary>
    /// Cancelling a single order.
    /// </summary>
    public const string OrderCancel = $"{OrderById}/cancel";

    /// <summary>
    /// Despatching a single order.
    /// </summary>
    public const string OrderShip = $"{OrderById}/ship";
}
