// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Tnosc.Lib.Infrastructure.Persistence.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.ReadModels;

/// <summary>
/// The query-side view of <c>ordering.orders</c>: flat primitives, no typed ids, no value objects.
/// </summary>
/// <remarks>
/// <see cref="Status"/> is a <see cref="string"/> here where the write model has an
/// <c>OrderStatus</c>. That is the read side doing its job — the column stores the enum's name, and a
/// DTO that carried the enum would invite a caller to branch on it. It is also what keeps this
/// projection translatable without dragging the write model's converters onto the query path.
/// </remarks>
internal sealed class OrderReadModel : IReadModel
{
    /// <summary>
    /// Gets the order's identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the order's human-facing reference.
    /// </summary>
    public string OrderNumber { get; init; } = null!;

    /// <summary>
    /// Gets the identifier of the customer who placed the order.
    /// </summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Gets the order's status, as its name.
    /// </summary>
    public string Status { get; init; } = null!;

    /// <summary>
    /// Gets the order's total after any discount.
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Gets the currency of the total.
    /// </summary>
    public string TotalCurrency { get; init; } = null!;

    /// <summary>
    /// Gets the UTC instant the order was placed.
    /// </summary>
    public DateTime PlacedOnUtc { get; init; }

    /// <summary>
    /// Gets the street line of the delivery address.
    /// </summary>
    public string ShippingStreet { get; init; } = null!;

    /// <summary>
    /// Gets the city of the delivery address.
    /// </summary>
    public string ShippingCity { get; init; } = null!;

    /// <summary>
    /// Gets the postal code of the delivery address.
    /// </summary>
    public string ShippingPostalCode { get; init; } = null!;

    /// <summary>
    /// Gets the ISO 3166-1 alpha-2 country code of the delivery address.
    /// </summary>
    public string ShippingCountry { get; init; } = null!;

    /// <summary>
    /// Gets the order's lines.
    /// </summary>
    /// <remarks>
    /// A navigation on the read side, where the write side owns them as a child collection. Both map
    /// the same two tables; this shape exists so a projection can reach the lines in one query rather
    /// than a second round trip.
    /// </remarks>
    public ICollection<OrderLineReadModel> Lines { get; init; } = [];
}
