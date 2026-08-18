// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;

/// <summary>
/// <see cref="Components.AdjustStockDialog"/>'s component service — the only place that touches
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.ICatalogApi"/> for that dialog.
/// </summary>
public interface IAdjustStockService
{
    /// <summary>Validates and submits a stock adjustment for a product.</summary>
    /// <param name="productId">The product to adjust.</param>
    /// <param name="viewModel">The dialog's current state.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> SubmitAsync(Guid productId, AdjustStockViewModel viewModel, CancellationToken cancellationToken);
}
