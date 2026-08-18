// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>The typed client for the Catalog bounded context's API.</summary>
public interface ICatalogApi
{
    /// <summary>Searches the product catalog.</summary>
    /// <param name="query">The search parameters.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<PagedResult<ProductSummary>>> SearchProductsAsync(
        SearchProductsQuery query,
        CancellationToken cancellationToken);

    /// <summary>Retrieves a single product by id.</summary>
    /// <param name="id">The product id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<Product>> GetProductAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Retrieves every catalog category.</summary>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<IReadOnlyList<Category>>> GetCategoriesAsync(CancellationToken cancellationToken);

    /// <summary>Adds a product to the catalogue.</summary>
    /// <param name="request">The product to create.</param>
    /// <param name="idempotencyKey">
    /// The idempotency key for this logical request. Callers own the key's lifetime — it must be
    /// stable across Polly's transport-level retries and rotated only once a response (success or
    /// business failure) actually arrives, never per HTTP send.
    /// </param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<Guid>> CreateProductAsync(
        CreateProductRequest request,
        Guid idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>Changes a product's price.</summary>
    /// <param name="productId">The product to reprice.</param>
    /// <param name="request">The new price.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> UpdateProductPriceAsync(
        Guid productId,
        UpdateProductPriceRequest request,
        CancellationToken cancellationToken);

    /// <summary>Adjusts a product's stock level by a signed delta.</summary>
    /// <param name="productId">The product to adjust.</param>
    /// <param name="request">The signed stock delta.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> AdjustStockAsync(
        Guid productId,
        AdjustStockRequest request,
        CancellationToken cancellationToken);
}
