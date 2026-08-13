// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// A single line of an <see cref="Order"/>: a product identifier plus a snapshot of that product's
/// data captured when the order was placed.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ProductId"/> is a plain <see cref="Guid"/> and everything else is a snapshot, never
/// a <c>Tnosc.EShop.Server.Domain.Catalog.Products.ProductId</c> and never a navigation to a product.
/// The same rule Basket follows, and for a stronger reason: <strong>an order must preserve the price
/// the customer actually paid</strong>. Pointing at the live product would let a catalogue reprice —
/// or a discontinuation, or a rename — silently rewrite an invoice that has already been settled.
/// </para>
/// <para>
/// A child <em>entity</em> rather than a value object because a line has identity within the order:
/// two lines for the same product at the same price are still two lines, and support refers to one of
/// them.
/// </para>
/// </remarks>
public sealed class OrderLine : Entity<OrderLineId>
{
    /// <summary>
    /// The maximum number of characters an order line's SKU snapshot may contain.
    /// </summary>
    public const int SkuMaxLength = 32;

    /// <summary>
    /// The maximum number of characters an order line's product-name snapshot may contain.
    /// </summary>
    public const int ProductNameMaxLength = 200;

    private OrderLine()
    {
        // EF.
    }

    /// <summary>
    /// Gets the identifier of the catalogue product this line snapshots.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Gets the product's stock-keeping unit, as it was when the order was placed.
    /// </summary>
    public string Sku { get; private set; } = null!;

    /// <summary>
    /// Gets the product's display name, as it was when the order was placed.
    /// </summary>
    public string ProductName { get; private set; } = null!;

    /// <summary>
    /// Gets the unit price the customer paid. Never rewritten by a later catalogue price change.
    /// </summary>
    public Money UnitPrice { get; private set; } = null!;

    /// <summary>
    /// Gets the number of units ordered on this line.
    /// </summary>
    public OrderQuantity Quantity { get; private set; } = null!;

    /// <summary>
    /// Gets what this line costs — <see cref="UnitPrice"/> times <see cref="Quantity"/>.
    /// </summary>
    /// <remarks>
    /// Computed rather than stored, so it can never disagree with the two values it is derived from.
    /// <c>OrderConfiguration</c> therefore ignores it.
    /// </remarks>
    public Money LineTotal => UnitPrice.Multiply(factor: Quantity.Value);

    /// <summary>
    /// Creates an order line from an already-validated snapshot.
    /// </summary>
    /// <remarks>
    /// <see langword="internal"/> because a line only exists as part of an order — the way in is
    /// <see cref="Order.Create"/>, which validates the drafts and owns the rules spanning the whole set
    /// (at least one line, one currency throughout).
    /// </remarks>
    /// <param name="productId">The identifier of the product this line snapshots.</param>
    /// <param name="sku">The product's stock-keeping unit at order time.</param>
    /// <param name="productName">The product's display name at order time.</param>
    /// <param name="unitPrice">The unit price the customer is paying.</param>
    /// <param name="quantity">The number of units ordered.</param>
    /// <returns>The created <see cref="OrderLine"/>.</returns>
    internal static OrderLine Create(
        Guid productId,
        string sku,
        string productName,
        Money unitPrice,
        OrderQuantity quantity) =>
        new()
        {
            Id = OrderLineId.New(),
            ProductId = productId,
            Sku = sku,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity,
        };
}
