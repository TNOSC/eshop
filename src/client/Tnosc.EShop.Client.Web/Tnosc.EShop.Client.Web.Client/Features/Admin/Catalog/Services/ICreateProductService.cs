// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;

/// <summary>
/// <see cref="Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Components.CreateProductDialog"/>'s
/// component service: validates and maps a <see cref="CreateProductViewModel"/> before calling the
/// Catalog API — the only place that touches
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.ICatalogApi"/> for this dialog.
/// </summary>
public interface ICreateProductService
{
    /// <summary>Loads the categories the dialog's category picker offers.</summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    Task<ClientResult<IReadOnlyList<CategoryViewModel>>> GetCategoriesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Validates <paramref name="viewModel"/>, maps it to a <c>CreateProductRequest</c> and submits
    /// it. A validation failure — client-side or the resulting server response — is returned as a
    /// failed <see cref="ClientResult{TValue}"/> rather than thrown.
    /// </summary>
    /// <param name="viewModel">The form's current state.</param>
    /// <param name="idempotencyKey">The caller-owned idempotency key for this submission attempt.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    Task<ClientResult<Guid>> SubmitAsync(
        CreateProductViewModel viewModel,
        Guid idempotencyKey,
        CancellationToken cancellationToken);
}
