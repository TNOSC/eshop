// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.EShop.Server.Domain.Basket.Baskets;

namespace Tnosc.EShop.Server.Application.Basket;

/// <summary>
/// Projects the <see cref="Tnosc.EShop.Server.Domain.Basket.Baskets.Basket"/> aggregate the write
/// handlers hold into the <see cref="BasketDto"/> they hand back to their callers, so the shape is
/// spelled once rather than once per command handler.
/// </summary>
internal static class BasketDtoMapper
{
    /// <summary>
    /// Projects a basket aggregate into its DTO.
    /// </summary>
    /// <param name="basket">The basket to project.</param>
    /// <returns>The projected <see cref="BasketDto"/>.</returns>
    public static BasketDto ToDto(this Tnosc.EShop.Server.Domain.Basket.Baskets.Basket basket) =>
        new(
            BasketId: basket.Id.Value,
            CustomerId: basket.CustomerId,
            Items: [.. basket.Items.Select(selector: static item => new BasketItemDto(
                ItemId: item.Id.Value,
                ProductId: item.ProductId,
                Sku: item.Sku,
                ProductName: item.ProductName,
                UnitPriceAmount: item.UnitPrice.Amount,
                UnitPriceCurrency: item.UnitPrice.Currency,
                Quantity: item.Quantity.Value))],
            TotalAmount: basket.Total?.Amount,
            TotalCurrency: basket.Total?.Currency);
}
