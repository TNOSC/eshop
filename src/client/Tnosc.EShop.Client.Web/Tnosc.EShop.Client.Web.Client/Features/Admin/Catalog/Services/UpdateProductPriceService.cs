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

/// <inheritdoc cref="IUpdateProductPriceService" />
internal sealed class UpdateProductPriceService(ICatalogApi catalogApi) : IUpdateProductPriceService
{
    public Task<ClientResult> SubmitAsync(Guid productId, UpdateProductPriceViewModel viewModel, CancellationToken cancellationToken)
    {
        ClientProblem? validation = ClientValidation.Validate(viewModel: viewModel);

        if (validation is not null)
        {
            return Task.FromResult(ClientResult.Failure(problem: validation));
        }

        return catalogApi.UpdateProductPriceAsync(
            productId: productId,
            request: new UpdateProductPriceRequest(Amount: viewModel.Amount, Currency: viewModel.Currency),
            cancellationToken: cancellationToken);
    }
}
