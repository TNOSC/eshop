// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Basket.Baskets;

/// <summary>
/// A customer's shopping basket. Owns every rule about which lines it holds and how their
/// quantities move.
/// </summary>
/// <remarks>
/// This aggregate does not live in Postgres — it is stored as one JSON document in Redis with a
/// sliding TTL, mapped by <c>BasketDocument</c> in <c>Server.Infrastructure.External</c>. That has
/// three consequences that hold everywhere in this type: it raises <b>no domain events</b> (an event
/// raised here would never become an outbox row, because only <c>UnitOfWork.SaveChangesAsync</c>
/// converts the EF change tracker's raised events, and a Redis-stored aggregate is never tracked by
/// it); <see cref="Rehydrate"/> restores the exact stored <c>Version</c> via <c>SetVersion</c> rather
/// than replaying transitions, because a Lua script in the repository compares that value against the
/// stored one for optimistic concurrency; and command handlers over this aggregate take no
/// <c>IUnitOfWork</c>, persisting instead through <see cref="IBasketRepository.SaveAsync"/> directly.
/// </remarks>
public sealed class Basket : AggregateRoot<BasketId>
{
    private readonly List<BasketItem> _items = [];

    private Basket()
    {
        // Rehydration.
    }

    /// <summary>
    /// Gets the identifier of the customer this basket belongs to.
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Gets the version this instance was loaded at — <c>0</c> for a basket that has never been
    /// persisted, or the stored <c>Version</c> at the time <see cref="Rehydrate"/> reconstructed it.
    /// </summary>
    /// <remarks>
    /// Deliberately distinct from <see cref="AggregateRoot{TEntityId}.Version"/>, which keeps
    /// incrementing with every transition applied since. <see cref="CreateFor"/> itself counts as a
    /// transition (per this codebase's "every state-changing method increments the version" rule), so
    /// a freshly created basket that then has an item added before its first save already carries
    /// <c>Version == 2</c> despite never having been written to Redis — <c>Version - 1</c> would
    /// therefore not match the (nonexistent) stored document. The repository's optimistic-concurrency
    /// check compares the stored document against this property instead, not against
    /// <c>Version - 1</c>.
    /// </remarks>
    public int OriginalVersion { get; private set; }

    /// <summary>
    /// Gets the basket's lines.
    /// </summary>
    public IReadOnlyCollection<BasketItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Gets the basket's total, or <see langword="null"/> when it has no lines. Assumes every line
    /// shares one currency — the only shape a single-storefront checkout in this solution produces.
    /// </summary>
    public Money? Total
    {
        get
        {
            if (_items.Count == 0)
            {
                return null;
            }

            Money total = _items[0].UnitPrice.Multiply(factor: _items[0].Quantity.Value);

            foreach (BasketItem item in _items.Skip(count: 1))
            {
                total = total.Add(other: item.UnitPrice.Multiply(factor: item.Quantity.Value)).Value;
            }

            return total;
        }
    }

    /// <summary>
    /// Creates an empty basket for the specified customer.
    /// </summary>
    /// <param name="customerId">The identifier of the customer the basket belongs to.</param>
    /// <returns>The created, empty <see cref="Basket"/>.</returns>
    public static Basket CreateFor(Guid customerId)
    {
        var basket = new Basket { Id = BasketId.New(), CustomerId = customerId };
        basket.IncrementVersion();

        return basket;
    }

    /// <summary>
    /// Adds a product snapshot to the basket, incrementing the existing line's quantity when that
    /// product is already present rather than adding a second line for it.
    /// </summary>
    /// <param name="productId">The identifier of the product being added.</param>
    /// <param name="sku">The product's stock-keeping unit, snapshotted at add-time.</param>
    /// <param name="productName">The product's display name, snapshotted at add-time.</param>
    /// <param name="unitPrice">The unit price the customer saw, snapshotted at add-time.</param>
    /// <param name="quantity">The quantity being added.</param>
    /// <returns>Success, or a <c>Quantity.OutOfRange</c> validation error when the combined quantity is out of range.</returns>
    public Result AddItem(
        Guid productId,
        string sku,
        string productName,
        Money unitPrice,
        Quantity quantity)
    {
        BasketItem? existing = _items.Find(match: item => item.ProductId == productId);

        if (existing is null)
        {
            _items.Add(item: BasketItem.Create(
                productId: productId,
                sku: sku,
                productName: productName,
                unitPrice: unitPrice,
                quantity: quantity));

            IncrementVersion();
            return Result.Success();
        }

        Result<Quantity> increased = existing.Quantity.Add(other: quantity);

        if (increased.IsError)
        {
            return increased.Errors.ToArray();
        }

        existing.ChangeQuantity(quantity: increased.Value);
        IncrementVersion();

        return Result.Success();
    }

    /// <summary>
    /// Replaces the quantity of an existing line.
    /// </summary>
    /// <param name="itemId">The identifier of the line to update.</param>
    /// <param name="quantity">The new quantity.</param>
    /// <returns>Success, or a <c>Basket.ItemNotFound</c> error when no line carries that identifier.</returns>
    public Result ChangeItemQuantity(BasketItemId itemId, Quantity quantity)
    {
        BasketItem? item = _items.Find(match: candidate => candidate.Id == itemId);

        if (item is null)
        {
            return BasketErrors.ItemNotFound(itemId: itemId.Value);
        }

        item.ChangeQuantity(quantity: quantity);
        IncrementVersion();

        return Result.Success();
    }

    /// <summary>
    /// Removes a line from the basket.
    /// </summary>
    /// <param name="itemId">The identifier of the line to remove.</param>
    /// <returns>Success, or a <c>Basket.ItemNotFound</c> error when no line carries that identifier.</returns>
    public Result RemoveItem(BasketItemId itemId)
    {
        BasketItem? item = _items.Find(match: candidate => candidate.Id == itemId);

        if (item is null)
        {
            return BasketErrors.ItemNotFound(itemId: itemId.Value);
        }

        _items.Remove(item: item);
        IncrementVersion();

        return Result.Success();
    }

    /// <summary>
    /// Empties the basket.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        IncrementVersion();
    }

    /// <summary>
    /// Reconstructs a basket from its stored document, restoring its exact version rather than
    /// recomputing one by replaying transitions. Raises no events.
    /// </summary>
    /// <param name="id">The basket's stored identifier.</param>
    /// <param name="customerId">The identifier of the customer the basket belongs to.</param>
    /// <param name="items">The basket's stored lines.</param>
    /// <param name="version">The basket's stored version, used for optimistic concurrency on the next write.</param>
    /// <returns>The reconstructed <see cref="Basket"/>.</returns>
    public static Basket Rehydrate(
        BasketId id,
        Guid customerId,
        IEnumerable<BasketItemSnapshot> items,
        int version)
    {
        var basket = new Basket { Id = id, CustomerId = customerId };
        basket._items.AddRange(collection: items.Select(selector: static snapshot => BasketItem.Rehydrate(
            id: snapshot.ItemId,
            productId: snapshot.ProductId,
            sku: snapshot.Sku,
            productName: snapshot.ProductName,
            unitPrice: snapshot.UnitPrice,
            quantity: snapshot.Quantity)));
        basket.SetVersion(version: version);
        basket.OriginalVersion = version;

        return basket;
    }
}
