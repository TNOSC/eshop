// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.Queries;

/// <summary>
/// One row of <see cref="GetProductByIdQueryHandler"/>'s raw-SQL result set.
/// </summary>
/// <remarks>
/// Every identifier is declared as <see cref="Guid"/>, never as a strongly-typed id. EF Core value
/// converters apply only to LINQ-translated queries; raw SQL bypasses them entirely, so a
/// <c>ProductId</c>-typed property here would fail to materialize. Property names match the quoted
/// column aliases in the query one for one.
/// </remarks>
internal sealed class ProductByIdRow
{
    /// <summary>
    /// Gets or sets the product's identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product's stock-keeping unit.
    /// </summary>
    public string Sku { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product's display name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product's optional long-form description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the product's current price amount.
    /// </summary>
    public decimal PriceAmount { get; set; }

    /// <summary>
    /// Gets or sets the product's current price currency.
    /// </summary>
    public string PriceCurrency { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of units currently on hand.
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the product's brand.
    /// </summary>
    public Guid BrandId { get; set; }

    /// <summary>
    /// Gets or sets the name of the product's brand.
    /// </summary>
    public string BrandName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the product's category.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the product has been withdrawn from sale.
    /// </summary>
    public bool IsDiscontinued { get; set; }
}
