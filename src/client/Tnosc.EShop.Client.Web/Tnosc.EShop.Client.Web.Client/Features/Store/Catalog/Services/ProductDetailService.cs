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
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Services;

/// <inheritdoc cref="IProductDetailService" />
internal sealed class ProductDetailService(ICatalogApi catalogApi, IBasketApi basketApi) : IProductDetailService
{
    public Task<ClientResult<Product>> GetProductAsync(Guid id, CancellationToken cancellationToken) =>
        catalogApi.GetProductAsync(id: id, cancellationToken: cancellationToken);

    public Task<ClientResult<BasketDto>> AddToBasketAsync(Guid productId, int quantity, CancellationToken cancellationToken) =>
        basketApi.AddItemAsync(
            request: new AddItemToBasketRequest(ProductId: productId, Quantity: quantity),
            cancellationToken: cancellationToken);
}
