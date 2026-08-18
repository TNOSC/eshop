// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Services;

/// <summary>
/// <see cref="Pages.ProductDetailPage"/>'s component service — the only place that touches
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.ICatalogApi"/> and
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.IBasketApi"/> for the product detail page.
/// </summary>
public interface IProductDetailService
{
    /// <summary>Retrieves a single product by id.</summary>
    /// <param name="id">The product id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<Product>> GetProductAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Adds a quantity of a product to the caller's basket.</summary>
    /// <param name="productId">The product to add.</param>
    /// <param name="quantity">The quantity to add.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<BasketDto>> AddToBasketAsync(Guid productId, int quantity, CancellationToken cancellationToken);
}
