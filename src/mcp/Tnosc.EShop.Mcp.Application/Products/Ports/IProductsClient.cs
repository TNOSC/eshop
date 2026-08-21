// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.EShop.Mcp.Application.Products.Ports;

/// <summary>
/// Calls the eShop catalog's products endpoint.
/// </summary>
public interface IProductsClient
{
    /// <summary>
    /// Fetches one page of products from the catalog, optionally filtered by a free-text search term.
    /// </summary>
    /// <param name="search">An optional free-text search term.</param>
    /// <param name="page">The one-based page number to fetch.</param>
    /// <param name="pageSize">The maximum number of products to fetch.</param>
    /// <param name="cancellationToken">The token used to cancel the request.</param>
    /// <returns>The products on the requested page.</returns>
    Task<IReadOnlyCollection<Product>> GetProductsAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new product to the catalog.
    /// </summary>
    /// <param name="request">The data describing the product to create.</param>
    /// <param name="cancellationToken">The token used to cancel the request.</param>
    /// <returns>The identifier of the newly created product.</returns>
    Task<Guid> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);
}
