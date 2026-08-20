// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Mcp.Application.Products;

/// <summary>
/// Retrieves products for MCP tools to expose. Owns the orchestration of the call to the eShop
/// catalog; a tool only validates its own parameters and delegates here.
/// </summary>
public interface IProductsQueryService
{
    /// <summary>
    /// Retrieves one page of products from the catalog, optionally filtered by a free-text search term.
    /// </summary>
    /// <param name="search">An optional free-text search term.</param>
    /// <param name="page">The one-based page number to fetch.</param>
    /// <param name="pageSize">The maximum number of products to fetch.</param>
    /// <param name="cancellationToken">The token used to cancel the request.</param>
    /// <returns>
    /// A successful result carrying the products on the requested page, or a failed result
    /// describing why the eShop catalog could not be reached or understood.
    /// </returns>
    Task<Result<IReadOnlyCollection<Product>>> GetProductsAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
