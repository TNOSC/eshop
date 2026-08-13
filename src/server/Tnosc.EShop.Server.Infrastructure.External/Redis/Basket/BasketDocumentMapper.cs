// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using Tnosc.EShop.Server.Domain.Basket.Baskets;
using Tnosc.EShop.Server.Domain.Shared;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Infrastructure.External.Redis.Basket;

/// <summary>
/// Maps between the domain <see cref="BasketAggregate"/> and its serialized <see cref="BasketDocument"/>.
/// </summary>
internal static class BasketDocumentMapper
{
    /// <summary>
    /// Projects a basket aggregate into its stored document shape.
    /// </summary>
    /// <param name="basket">The basket to project.</param>
    /// <returns>The equivalent <see cref="BasketDocument"/>.</returns>
    public static BasketDocument ToDocument(this BasketAggregate basket) =>
        new(
            BasketId: basket.Id.Value,
            CustomerId: basket.CustomerId,
            Items: [.. basket.Items.Select(selector: static item => new BasketItemDocument(
                ItemId: item.Id.Value,
                ProductId: item.ProductId,
                Sku: item.Sku,
                ProductName: item.ProductName,
                UnitPriceAmount: item.UnitPrice.Amount,
                UnitPriceCurrency: item.UnitPrice.Currency,
                Quantity: item.Quantity.Value))],
            Version: basket.Version);

    /// <summary>
    /// Reconstructs a basket aggregate from its stored document.
    /// </summary>
    /// <param name="document">The stored document to reconstruct from.</param>
    /// <returns>The reconstructed <see cref="BasketAggregate"/>.</returns>
    public static BasketAggregate ToBasket(this BasketDocument document) =>
        BasketAggregate.Rehydrate(
            id: BasketId.From(value: document.BasketId),
            customerId: document.CustomerId,
            items: document.Items.Select(selector: static item => new BasketItemSnapshot(
                ItemId: BasketItemId.From(value: item.ItemId),
                ProductId: item.ProductId,
                Sku: item.Sku,
                ProductName: item.ProductName,
                UnitPrice: Money.Create(amount: item.UnitPriceAmount, currency: item.UnitPriceCurrency).Value,
                Quantity: Quantity.Create(value: item.Quantity).Value)),
            version: document.Version);
}
