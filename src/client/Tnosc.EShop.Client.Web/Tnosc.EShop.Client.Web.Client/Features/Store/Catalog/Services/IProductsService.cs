// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Services;

/// <summary>
/// The storefront product gallery's component service — the only place that touches
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.ICatalogApi"/> for the storefront
/// product gallery.
/// </summary>
public interface IProductsService
{
    /// <summary>Loads the categories offered by <c>ProductFilters</c>.</summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    Task<ClientResult<IReadOnlyList<Category>>> GetCategoriesAsync(CancellationToken cancellationToken);

    /// <summary>Searches products using <paramref name="viewModel"/>'s current filter/page state.</summary>
    /// <param name="viewModel">The gallery's current state.</param>
    /// <param name="pageSize">The page size the caller renders.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    Task<ClientResult<PagedResult<ProductSummary>>> SearchAsync(
        ProductsViewModel viewModel,
        int pageSize,
        CancellationToken cancellationToken);
}
