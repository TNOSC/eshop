// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Basket.Baskets;

/// <summary>
/// A single line in a <see cref="Basket"/>: a product identifier plus a snapshot of that product's
/// data captured when the line was created.
/// </summary>
/// <remarks>
/// Deliberately a plain <see cref="Guid"/> product id and a snapshot, never a
/// <c>Tnosc.EShop.Server.Domain.Catalog.Products.ProductId</c> and never a navigation to a product —
/// Basket must not reference Catalog. This is correct modelling, not a workaround: a basket line must
/// preserve the price the customer saw. Pointing at the live product would let a catalogue price
/// change silently rewrite baskets, and later order history.
/// </remarks>
public sealed class BasketItem : Entity<BasketItemId>
{
    private BasketItem()
    {
        // Rehydration.
    }

    /// <summary>
    /// Gets the identifier of the catalogue product this line snapshots.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Gets the product's stock-keeping unit, as it was when this line was added.
    /// </summary>
    public string Sku { get; private set; } = null!;

    /// <summary>
    /// Gets the product's display name, as it was when this line was added.
    /// </summary>
    public string ProductName { get; private set; } = null!;

    /// <summary>
    /// Gets the unit price the customer saw when this line was added. Never rewritten by a later
    /// catalogue price change.
    /// </summary>
    public Money UnitPrice { get; private set; } = null!;

    /// <summary>
    /// Gets the number of units of this line.
    /// </summary>
    public Quantity Quantity { get; private set; } = null!;

    /// <summary>
    /// Creates a new basket line from a product snapshot.
    /// </summary>
    /// <param name="productId">The identifier of the product this line snapshots.</param>
    /// <param name="sku">The product's stock-keeping unit at the time of adding.</param>
    /// <param name="productName">The product's display name at the time of adding.</param>
    /// <param name="unitPrice">The unit price the customer saw at the time of adding.</param>
    /// <param name="quantity">The line's initial quantity.</param>
    /// <returns>The created <see cref="BasketItem"/>.</returns>
    internal static BasketItem Create(
        Guid productId,
        string sku,
        string productName,
        Money unitPrice,
        Quantity quantity) =>
        new()
        {
            Id = BasketItemId.New(),
            ProductId = productId,
            Sku = sku,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity,
        };

    /// <summary>
    /// Reconstructs a basket line from a stored snapshot, without raising events.
    /// </summary>
    /// <param name="id">The line's stored identifier.</param>
    /// <param name="productId">The stored product identifier.</param>
    /// <param name="sku">The stored SKU snapshot.</param>
    /// <param name="productName">The stored product name snapshot.</param>
    /// <param name="unitPrice">The stored unit price snapshot.</param>
    /// <param name="quantity">The stored quantity.</param>
    /// <returns>The reconstructed <see cref="BasketItem"/>.</returns>
    internal static BasketItem Rehydrate(
        BasketItemId id,
        Guid productId,
        string sku,
        string productName,
        Money unitPrice,
        Quantity quantity) =>
        new()
        {
            Id = id,
            ProductId = productId,
            Sku = sku,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity,
        };

    /// <summary>
    /// Replaces this line's quantity. Called only by <see cref="Basket"/>, which owns the transition
    /// and its versioning.
    /// </summary>
    /// <param name="quantity">The new quantity.</param>
    internal void ChangeQuantity(Quantity quantity) =>
        Quantity = quantity;
}
