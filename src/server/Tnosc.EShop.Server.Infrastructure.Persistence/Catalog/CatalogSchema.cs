// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog;

/// <summary>
/// Names the Postgres objects owned by the Catalog bounded context. One schema per context keeps the
/// contexts separable inside the single database the outbox forces them to share.
/// </summary>
internal static class CatalogSchema
{
    /// <summary>
    /// The Postgres schema every Catalog table lives in.
    /// </summary>
    public const string Name = "catalog";

    /// <summary>
    /// The name of the products table.
    /// </summary>
    public const string ProductsTable = "products";

    /// <summary>
    /// The name of the brands table.
    /// </summary>
    public const string BrandsTable = "brands";

    /// <summary>
    /// The name of the categories table.
    /// </summary>
    public const string CategoriesTable = "categories";
}
