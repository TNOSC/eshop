// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Basket.Ports;
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.EShop.Server.Shared.Basket;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Basket.Queries;

/// <summary>
/// Projects the caller's basket from <see cref="IBasketReader"/> — no Redis code here, only the
/// mapping from the port's snapshot shape to <see cref="BasketDto"/>. A customer with no basket yet
/// gets back an empty one rather than a <c>NotFound</c>: the basket is created lazily on first add.
/// </summary>
/// <param name="reader">The basket read port.</param>
[Cacheable(60)]
[CacheTag(CacheTags.Basket)]
internal sealed class GetBasketQueryHandler(IBasketReader reader)
    : IQueryHandler<GetBasketQuery, BasketDto>
{
    /// <inheritdoc />
    public async ValueTask<Result<BasketDto>> HandleAsync(
        GetBasketQuery query,
        CancellationToken cancellationToken = default)
    {
        BasketSnapshot? snapshot = await reader.ReadAsync(
            customerId: query.CustomerId,
            cancellationToken: cancellationToken);

        if (snapshot is null)
        {
            return new BasketDto(
                BasketId: Guid.Empty,
                CustomerId: query.CustomerId,
                Items: [],
                TotalAmount: null,
                TotalCurrency: null);
        }

        return new BasketDto(
            BasketId: snapshot.BasketId,
            CustomerId: snapshot.CustomerId,
            Items: [.. snapshot.Items.Select(selector: static line => new BasketItemDto(
                ItemId: line.ItemId,
                ProductId: line.ProductId,
                Sku: line.Sku,
                ProductName: line.ProductName,
                UnitPriceAmount: line.UnitPriceAmount,
                UnitPriceCurrency: line.UnitPriceCurrency,
                Quantity: line.Quantity))],
            TotalAmount: snapshot.TotalAmount,
            TotalCurrency: snapshot.TotalCurrency);
    }
}
