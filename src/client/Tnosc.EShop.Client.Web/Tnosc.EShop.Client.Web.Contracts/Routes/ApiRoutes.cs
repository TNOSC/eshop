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
    }
}
