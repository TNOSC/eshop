// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Infrastructure.Persistence.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.ReadModels;

/// <summary>
/// The query-side view of <c>catalog.products</c>: flat primitives, no typed ids, no value objects.
/// </summary>
/// <remarks>
/// The read model deliberately does not reuse the <c>Product</c> aggregate. The read context maps
/// read models only, and a flat shape keeps projections translatable without dragging the write
/// model's owned types and value converters onto the query path.
/// </remarks>
internal sealed class ProductReadModel : IReadModel
{
    /// <summary>
    /// Gets the product's identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the product's stock-keeping unit.
    /// </summary>
    public string Sku { get; init; } = null!;

    /// <summary>
    /// Gets the product's display name.
    /// </summary>
    public string Name { get; init; } = null!;

    /// <summary>
    /// Gets the product's optional long-form description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the product's current price amount.
    /// </summary>
    public decimal PriceAmount { get; init; }

    /// <summary>
    /// Gets the product's current price currency.
    /// </summary>
    public string PriceCurrency { get; init; } = null!;

    /// <summary>
    /// Gets the number of units currently on hand.
    /// </summary>
    public int StockQuantity { get; init; }

    /// <summary>
    /// Gets the identifier of the product's brand.
    /// </summary>
    public Guid BrandId { get; init; }

    /// <summary>
    /// Gets the identifier of the product's category.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the product has been withdrawn from sale.
    /// </summary>
    public bool IsDiscontinued { get; init; }

    /// <summary>
    /// Gets the URL of the product's image, or <see langword="null"/> when none has been uploaded.
    /// </summary>
    public string? ImageUrl { get; init; }
}
