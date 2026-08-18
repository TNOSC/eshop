// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Validation;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;

/// <inheritdoc cref="IAdjustStockService" />
internal sealed class AdjustStockService(ICatalogApi catalogApi) : IAdjustStockService
{
    public Task<ClientResult> SubmitAsync(Guid productId, AdjustStockViewModel viewModel, CancellationToken cancellationToken)
    {
        ClientProblem? validation = ClientValidation.Validate(viewModel: viewModel);

        if (validation is not null)
        {
            return Task.FromResult(ClientResult.Failure(problem: validation));
        }

        return catalogApi.AdjustStockAsync(
            productId: productId,
            request: new AdjustStockRequest(Delta: viewModel.Delta),
            cancellationToken: cancellationToken);
    }
}
