// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Validation;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;

/// <inheritdoc cref="ICreateProductService" />
internal sealed class CreateProductService(ICatalogApi catalogApi) : ICreateProductService
{
    private static readonly ClientProblem InvalidBrandIdProblem = new(
        Type: null,
        Title: "Validation failed",
        Status: 400,
        Detail: null,
        Instance: null,
        Errors: new Dictionary<string, string[]>(comparer: StringComparer.Ordinal)
        {
            [nameof(CreateProductViewModel.BrandId)] = ["The brand id must be a valid GUID."],
        },
        ErrorCode: ClientValidation.ValidationErrorCode,
        TraceId: null);

    public async Task<ClientResult<IReadOnlyList<CategoryViewModel>>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        ClientResult<IReadOnlyList<Category>> result = await catalogApi.GetCategoriesAsync(cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ClientResult<IReadOnlyList<CategoryViewModel>>.Failure(problem: result.Problem!);
        }

        IReadOnlyList<CategoryViewModel> categories = [.. result.Value.Select(ToViewModel)];
        return ClientResult<IReadOnlyList<CategoryViewModel>>.Success(value: categories);
    }

    public async Task<ClientResult<Guid>> SubmitAsync(
        CreateProductViewModel viewModel,
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        ClientProblem? validation = ClientValidation.Validate(viewModel: viewModel);

        if (validation is not null)
        {
            return ClientResult<Guid>.Failure(problem: validation);
        }

        if (!Guid.TryParse(input: viewModel.BrandId, result: out Guid brandId))
        {
            return ClientResult<Guid>.Failure(problem: InvalidBrandIdProblem);
        }

        CreateProductRequest request = new(
            Sku: viewModel.Sku,
            Name: viewModel.Name,
            Description: string.IsNullOrWhiteSpace(value: viewModel.Description) ? null : viewModel.Description,
            PriceAmount: viewModel.PriceAmount,
            PriceCurrency: viewModel.PriceCurrency,
            StockQuantity: viewModel.StockQuantity,
            BrandId: brandId,
            CategoryId: viewModel.CategoryId!.Value);

        return await catalogApi.CreateProductAsync(
            request: request,
            idempotencyKey: idempotencyKey,
            cancellationToken: cancellationToken);
    }

    private static CategoryViewModel ToViewModel(Category category) =>
        new() { Id = category.Id, Name = category.Name };
}
