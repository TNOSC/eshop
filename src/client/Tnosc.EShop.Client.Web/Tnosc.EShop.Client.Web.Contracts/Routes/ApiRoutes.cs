// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Tnosc.EShop.Client.Web.Contracts.Catalog;

namespace Tnosc.EShop.Client.Web.Contracts.Routes;

/// <summary>
/// Every API route the client calls, as relative paths — deliberately without a leading slash, since
/// a leading slash discards the BFF base address's path segment when combined with
/// <see cref="Uri"/>.
/// </summary>
public static class ApiRoutes
{
    /// <summary>
    /// Agent host routes. These sit under the BFF's <c>agents/</c> prefix rather than <c>api/</c>,
    /// because the agent host is a separate downstream service from the eShop API.
    /// </summary>
    public static class Agent
    {
        /// <summary>The shopping assistant's AG-UI conversation endpoint.</summary>
        public const string ShoppingAssistant = "agents/shopping-assistant";
    }

    /// <summary>Catalog bounded context routes.</summary>
    public static class Catalog
    {
        /// <summary>The products collection route.</summary>
        public const string Products = "api/catalog/products";

        /// <summary>The categories collection route.</summary>
        public const string Categories = "api/catalog/categories";

        /// <summary>Builds the route for a single product by id.</summary>
        /// <param name="id">The product id.</param>
        public static string ProductById(Guid id) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Products}/{id}");

        /// <summary>Builds the route for changing a product's price.</summary>
        /// <param name="id">The product id.</param>
        public static string ProductPrice(Guid id) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Products}/{id}/price");

        /// <summary>Builds the route for adjusting a product's stock.</summary>
        /// <param name="id">The product id.</param>
        public static string ProductStock(Guid id) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Products}/{id}/stock");

        /// <summary>Builds the products route with a search query string appended.</summary>
        /// <param name="query">The search parameters.</param>
        public static string SearchProducts(SearchProductsQuery query)
        {
            List<string> parameters = [];

            if (!string.IsNullOrWhiteSpace(value: query.Search))
            {
                parameters.Add(item: string.Create(
                    provider: CultureInfo.InvariantCulture,
                    handler: $"search={Uri.EscapeDataString(stringToEscape: query.Search)}"));
            }

            if (query.CategoryId is { } categoryId)
            {
                parameters.Add(item: string.Create(
                    provider: CultureInfo.InvariantCulture,
                    handler: $"categoryId={categoryId}"));
            }

            parameters.Add(item: string.Create(
                provider: CultureInfo.InvariantCulture,
                handler: $"page={query.Page}"));
            parameters.Add(item: string.Create(
                provider: CultureInfo.InvariantCulture,
                handler: $"pageSize={query.PageSize}"));

            StringBuilder builder = new(value: Products);
            builder.Append(value: '?');
            builder.Append(value: string.Join(separator: '&', values: parameters));
            return builder.ToString();
        }
    }

    /// <summary>Identity bounded context routes.</summary>
    public static class Identity
    {
        /// <summary>The customers collection route.</summary>
        public const string Customers = "api/identity/customers";

        /// <summary>The current caller's customer profile route.</summary>
        public const string Me = "api/identity/customers/me";

        /// <summary>The current caller's editable profile route.</summary>
        public const string MeProfile = "api/identity/customers/me/profile";

        /// <summary>The current caller's addresses route.</summary>
        public const string MeAddresses = "api/identity/customers/me/addresses";

        /// <summary>Builds the route for a single one of the caller's own addresses.</summary>
        /// <param name="addressId">The address id.</param>
        public static string MeAddressById(Guid addressId) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{MeAddresses}/{addressId}");

        /// <summary>Builds the route to set which of the caller's own addresses is the default.</summary>
        /// <param name="addressId">The address id.</param>
        public static string MeDefaultAddressById(Guid addressId) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{MeAddresses}/{addressId}/default");

        /// <summary>Builds the route for a single customer by id.</summary>
        /// <param name="id">The customer id.</param>
        public static string CustomerById(Guid id) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Customers}/{id}");

        /// <summary>Builds the route for an admin update of a customer's profile.</summary>
        /// <param name="id">The customer id.</param>
        public static string CustomerProfileById(Guid id) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Customers}/{id}/profile");

        /// <summary>Builds the route for an admin operation on a customer's addresses.</summary>
        /// <param name="id">The customer id.</param>
        public static string CustomerAddressesById(Guid id) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Customers}/{id}/addresses");

        /// <summary>Builds the route for a single address on a customer, as an admin.</summary>
        /// <param name="id">The customer id.</param>
        /// <param name="addressId">The address id.</param>
        public static string CustomerAddressByIdAndAddressId(Guid id, Guid addressId) =>
            string.Create(
                provider: CultureInfo.InvariantCulture,
                handler: $"{Customers}/{id}/addresses/{addressId}");

        /// <summary>Builds the route to set a customer's default address, as an admin.</summary>
        /// <param name="id">The customer id.</param>
        /// <param name="addressId">The address id.</param>
        public static string CustomerDefaultAddressById(Guid id, Guid addressId) =>
            string.Create(
                provider: CultureInfo.InvariantCulture,
                handler: $"{Customers}/{id}/addresses/{addressId}/default");

        /// <summary>Builds the route to deactivate a customer, as an admin.</summary>
        /// <param name="id">The customer id.</param>
        public static string CustomerDeactivateById(Guid id) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Customers}/{id}/deactivate");

        /// <summary>Builds the customers route with paging and filter parameters appended.</summary>
        /// <param name="search">An optional free-text search term.</param>
        /// <param name="isActive">An optional active-status filter.</param>
        /// <param name="page">The requested page.</param>
        /// <param name="pageSize">The requested page size.</param>
        public static string SearchCustomers(string? search, bool? isActive, int page, int pageSize)
        {
            List<string> parameters = [];

            if (!string.IsNullOrWhiteSpace(value: search))
            {
                parameters.Add(item: string.Create(
                    provider: CultureInfo.InvariantCulture,
                    handler: $"search={Uri.EscapeDataString(stringToEscape: search)}"));
            }

            if (isActive is { } active)
            {
                parameters.Add(item: string.Create(
                    provider: CultureInfo.InvariantCulture,
                    handler: $"isActive={active}"));
            }

            parameters.Add(item: string.Create(provider: CultureInfo.InvariantCulture, handler: $"page={page}"));
            parameters.Add(item: string.Create(
                provider: CultureInfo.InvariantCulture,
                handler: $"pageSize={pageSize}"));

            StringBuilder builder = new(value: Customers);
            builder.Append(value: '?');
            builder.Append(value: string.Join(separator: '&', values: parameters));
            return builder.ToString();
        }
    }

    /// <summary>Basket bounded context routes.</summary>
    public static class Basket
    {
        /// <summary>The current caller's basket route.</summary>
        public const string Current = "api/basket";

        /// <summary>The current caller's basket items route.</summary>
        public const string Items = "api/basket/items";

        /// <summary>Builds the route for a single basket item by id.</summary>
        /// <param name="itemId">The basket item id.</param>
        public static string ItemById(Guid itemId) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Items}/{itemId}");
    }

    /// <summary>Ordering bounded context routes.</summary>
    public static class Ordering
    {
        /// <summary>The orders collection route.</summary>
        public const string Orders = "api/orders";

        /// <summary>Builds the route for a single order by id.</summary>
        /// <param name="id">The order id.</param>
        public static string OrderById(Guid id) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Orders}/{id}");

        /// <summary>Builds the route to confirm an order.</summary>
        /// <param name="id">The order id.</param>
        public static string OrderConfirm(Guid id) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Orders}/{id}/confirm");

        /// <summary>Builds the route to cancel an order.</summary>
        /// <param name="id">The order id.</param>
        public static string OrderCancel(Guid id) =>
            string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Orders}/{id}/cancel");

        /// <summary>Builds the orders route with paging parameters appended.</summary>
        /// <param name="page">The requested page.</param>
        /// <param name="pageSize">The requested page size.</param>
        public static string MyOrders(int page, int pageSize) =>
            string.Create(
                provider: CultureInfo.InvariantCulture,
                handler: $"{Orders}?page={page}&pageSize={pageSize}");
    }
}
