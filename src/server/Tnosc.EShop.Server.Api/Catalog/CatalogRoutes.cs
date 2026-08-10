// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Api.Catalog;

/// <summary>
/// The route templates and OpenAPI tag shared by the Catalog endpoints, so a path is spelled once.
/// </summary>
internal static class CatalogRoutes
{
    /// <summary>
    /// The OpenAPI tag every Catalog endpoint is grouped under.
    /// </summary>
    public const string Tag = "Catalog";

    /// <summary>
    /// The products collection.
    /// </summary>
    public const string Products = "/api/catalog/products";

    /// <summary>
    /// A single product by identifier.
    /// </summary>
    public const string ProductById = $"{Products}/{{id:guid}}";

    /// <summary>
    /// A single product's price.
    /// </summary>
    public const string ProductPrice = $"{ProductById}/price";

    /// <summary>
    /// A single product's stock level.
    /// </summary>
    public const string ProductStock = $"{ProductById}/stock";

    /// <summary>
    /// The categories collection.
    /// </summary>
    public const string Categories = "/api/catalog/categories";
}
