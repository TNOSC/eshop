// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Basket;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Basket.Services;

/// <inheritdoc cref="IBasketPageService" />
internal sealed class BasketPageService(IBasketApi basketApi) : IBasketPageService
{
    public Task<ClientResult<BasketDto>> GetBasketAsync(CancellationToken cancellationToken) =>
        basketApi.GetBasketAsync(cancellationToken: cancellationToken);

    public Task<ClientResult<BasketDto>> ChangeQuantityAsync(Guid itemId, int quantity, CancellationToken cancellationToken) =>
        basketApi.ChangeItemQuantityAsync(
            itemId: itemId,
            request: new ChangeBasketItemQuantityRequest(Quantity: quantity),
            cancellationToken: cancellationToken);

    public Task<ClientResult> RemoveItemAsync(Guid itemId, CancellationToken cancellationToken) =>
        basketApi.RemoveItemAsync(itemId: itemId, cancellationToken: cancellationToken);
}
