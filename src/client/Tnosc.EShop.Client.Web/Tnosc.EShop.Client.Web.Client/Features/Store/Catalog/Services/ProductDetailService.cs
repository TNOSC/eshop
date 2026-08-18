// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Basket;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Services;

/// <inheritdoc cref="IProductDetailService" />
internal sealed class ProductDetailService(ICatalogApi catalogApi, IBasketApi basketApi) : IProductDetailService
{
    public async Task<ClientResult<ProductDetailViewModel>> GetProductAsync(Guid id, CancellationToken cancellationToken)
    {
        ClientResult<Product> result = await catalogApi.GetProductAsync(id: id, cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ClientResult<ProductDetailViewModel>.Failure(problem: result.Problem!);
        }

        return ClientResult<ProductDetailViewModel>.Success(value: ToViewModel(product: result.Value));
    }

    public async Task<ClientResult<int>> AddToBasketAsync(Guid productId, int quantity, CancellationToken cancellationToken)
    {
        ClientResult<BasketDto> result = await basketApi.AddItemAsync(
            request: new AddItemToBasketRequest(ProductId: productId, Quantity: quantity),
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ClientResult<int>.Failure(problem: result.Problem!);
        }

        return ClientResult<int>.Success(value: result.Value.Items.Count);
    }

    private static ProductDetailViewModel ToViewModel(Product product) =>
        new()
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            PriceAmount = product.PriceAmount,
            PriceCurrency = product.PriceCurrency,
            StockQuantity = product.StockQuantity,
            BrandName = product.BrandName,
        };
}
