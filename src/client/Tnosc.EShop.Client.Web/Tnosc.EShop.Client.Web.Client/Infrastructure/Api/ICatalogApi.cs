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
using Tnosc.Lib.Web.Api;
using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>The typed client for the Catalog bounded context's API.</summary>
public interface ICatalogApi
{
    /// <summary>Searches the product catalog.</summary>
    /// <param name="query">The search parameters.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ApiResult<PagedResult<ProductSummary>>> SearchProductsAsync(
        SearchProductsQuery query,
        CancellationToken cancellationToken);

    /// <summary>Retrieves a single product by id.</summary>
    /// <param name="id">The product id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ApiResult<Product>> GetProductAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Retrieves every catalog category.</summary>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ApiResult<IReadOnlyList<Category>>> GetCategoriesAsync(CancellationToken cancellationToken);
}
