// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client;

/// <summary>
/// The single source of truth for every route in the storefront and back office. A page's own
/// <c>@page</c> directive still carries its route as a literal — Razor requires that — but every
/// nav link and every in-code navigation uses these members instead of a second, independent
/// literal, so a route only has one place to change.
/// </summary>
public static class Routes
{
    /// <summary>Storefront routes.</summary>
    public static class Store
    {
        /// <summary>The catalogue's default route.</summary>
        public const string Home = "/";

        /// <summary>The catalogue's explicit route.</summary>
        public const string Products = "/products";

        /// <summary>Builds the route to a single product's detail page.</summary>
        public static string ProductDetail(Guid id) => $"/products/{id}";

        /// <summary>The caller's basket.</summary>
        public const string Basket = "/basket";

        /// <summary>Checkout.</summary>
        public const string Checkout = "/checkout";

        /// <summary>The caller's order history.</summary>
        public const string Orders = "/orders";

        /// <summary>Builds the route to a single order's detail page.</summary>
        public static string OrderDetail(Guid id) => $"/orders/{id}";
    }

    /// <summary>Back-office routes.</summary>
    public static class Admin
    {
        /// <summary>The back-office landing page.</summary>
        public const string Dashboard = "/admin";

        /// <summary>The admin product console.</summary>
        public const string Products = "/admin/products";

        /// <summary>The admin customer console.</summary>
        public const string Customers = "/admin/customers";

        /// <summary>Builds the route to a single customer's detail page.</summary>
        public static string CustomerDetail(Guid id) => $"/admin/customers/{id}";
    }
}
