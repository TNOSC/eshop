// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Tnosc.EShop.Server.Tests.Acceptance.Contracts;

/// <summary>
/// The paths, credentials and well-known values the journeys drive the API with.
/// </summary>
/// <remarks>
/// Spelled out here rather than taken from <c>Server.Api</c>'s <c>*Routes</c> constants on purpose:
/// an acceptance test that imported the server's own route constants could not catch a path changing
/// under a client. These are a second, independent statement of the same contract — the client's
/// half — and the same reasoning applies to <see cref="FeaturedSku"/> and the realm credentials.
/// Drift here fails loudly on the next run, which is the difference between this and the silent
/// failure <c>.claude/rules/cache-tags.md</c> is about.
/// </remarks>
public static class AcceptanceRoutes
{
    /// <summary>The realm user holding the <c>customer</c> role.</summary>
    public const string CustomerUsername = "customer@eshop.local";

    /// <summary>The realm user holding the <c>admin</c> role.</summary>
    public const string AdminUsername = "admin@eshop.local";

    /// <summary>The password both realm users are imported with.</summary>
    public const string Password = "Passw0rd!";

    /// <summary>The SKU the seeded catalogue guarantees, and the journeys buy.</summary>
    public const string FeaturedSku = "TNOSC-LAPTOP-13";

    /// <summary>The gateway's always-declining test card.</summary>
    public const string DecliningCardNumber = "4000000000000002";

    /// <summary>A page of the catalogue big enough to hold everything the seeder writes.</summary>
    public const string CatalogProductsPage = "/api/catalog/products?pageSize=50";

    /// <summary>The caller's own customer profile collection.</summary>
    public const string Customers = "/api/identity/customers";

    /// <summary>The caller's own address collection.</summary>
    public const string CurrentCustomerAddresses = "/api/identity/customers/me/addresses";

    /// <summary>The caller's own basket.</summary>
    public const string Basket = "/api/basket";

    /// <summary>The caller's own basket lines.</summary>
    public const string BasketItems = "/api/basket/items";

    /// <summary>The caller's own orders.</summary>
    public const string Orders = "/api/orders";

    /// <summary>Payments in general — initiating one is a <c>POST</c> here.</summary>
    public const string Payments = "/api/payments";

    /// <summary>
    /// The route of a single order.
    /// </summary>
    /// <param name="orderId">The order's identifier.</param>
    /// <returns>The path <c>GET /api/orders/{id}</c> is served on.</returns>
    public static string OrderById(Guid orderId) =>
        string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Orders}/{orderId}");

    /// <summary>
    /// The route that confirms a single order.
    /// </summary>
    /// <param name="orderId">The order's identifier.</param>
    /// <returns>The path <c>POST /api/orders/{id}/confirm</c> is served on.</returns>
    public static string OrderConfirm(Guid orderId) =>
        string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Orders}/{orderId}/confirm");

    /// <summary>
    /// The route of the payment opened for a single order.
    /// </summary>
    /// <param name="orderId">The order's identifier.</param>
    /// <returns>The path <c>GET /api/orders/{orderId}/payment</c> is served on.</returns>
    public static string PaymentByOrder(Guid orderId) =>
        string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Orders}/{orderId}/payment");
}
