// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Store.Basket.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Basket;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Basket.Services;

/// <inheritdoc cref="IBasketPageService" />
internal sealed class BasketPageService(IBasketApi basketApi) : IBasketPageService
{
    public async Task<ClientResult<BasketViewModel>> GetBasketAsync(CancellationToken cancellationToken)
    {
        ClientResult<BasketDto> result = await basketApi.GetBasketAsync(cancellationToken: cancellationToken);
        return ToClientResult(result: result);
    }

    public async Task<ClientResult<BasketViewModel>> ChangeQuantityAsync(Guid itemId, int quantity, CancellationToken cancellationToken)
    {
        ClientResult<BasketDto> result = await basketApi.ChangeItemQuantityAsync(
            itemId: itemId,
            request: new ChangeBasketItemQuantityRequest(Quantity: quantity),
            cancellationToken: cancellationToken);

        return ToClientResult(result: result);
    }

    public Task<ClientResult> RemoveItemAsync(Guid itemId, CancellationToken cancellationToken) =>
        basketApi.RemoveItemAsync(itemId: itemId, cancellationToken: cancellationToken);

    private static ClientResult<BasketViewModel> ToClientResult(ClientResult<BasketDto> result)
    {
        if (!result.IsSuccess)
        {
            return ClientResult<BasketViewModel>.Failure(problem: result.Problem!);
        }

        return ClientResult<BasketViewModel>.Success(value: ToViewModel(basket: result.Value));
    }

    private static BasketViewModel ToViewModel(BasketDto basket) =>
        new()
        {
            Items = [.. basket.Items.Select(ToViewModel)],
            TotalAmount = basket.TotalAmount,
            TotalCurrency = basket.TotalCurrency,
        };

    private static BasketItemViewModel ToViewModel(BasketItem item) =>
        new()
        {
            ItemId = item.ItemId,
            Sku = item.Sku,
            ProductName = item.ProductName,
            UnitPriceAmount = item.UnitPriceAmount,
            UnitPriceCurrency = item.UnitPriceCurrency,
            Quantity = item.Quantity,
        };
}
