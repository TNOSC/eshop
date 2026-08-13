// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Infrastructure.Persistence.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.ReadModels;

/// <summary>
/// The query-side view of <c>ordering.order_lines</c>: flat primitives, no typed ids, no value objects.
/// </summary>
internal sealed class OrderLineReadModel : IReadModel
{
    /// <summary>
    /// Gets the line's identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the identifier of the order the line belongs to.
    /// </summary>
    public Guid OrderId { get; init; }

    /// <summary>
    /// Gets the identifier of the product ordered.
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// Gets the product's stock-keeping unit at order time.
    /// </summary>
    public string Sku { get; init; } = null!;

    /// <summary>
    /// Gets the product's display name at order time.
    /// </summary>
    public string ProductName { get; init; } = null!;

    /// <summary>
    /// Gets the unit price the customer paid.
    /// </summary>
    public decimal UnitPriceAmount { get; init; }

    /// <summary>
    /// Gets the currency of the unit price.
    /// </summary>
    public string UnitPriceCurrency { get; init; } = null!;

    /// <summary>
    /// Gets the number of units ordered.
    /// </summary>
    public int Quantity { get; init; }
}
