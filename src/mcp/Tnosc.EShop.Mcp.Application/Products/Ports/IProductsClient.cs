// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.EShop.Mcp.Application.Products.Ports;

/// <summary>
/// Fetches products from the eShop catalog's search endpoint.
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
}
