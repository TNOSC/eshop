// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Mcp.Application.Products.Ports;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Mcp.Application.Products;

/// <inheritdoc cref="IProductsCommandService" />
internal sealed class ProductsCommandService(IProductsClient productsClient) : IProductsCommandService
{
    /// <inheritdoc />
    public async Task<Result<Guid>> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Guid productId = await productsClient.CreateProductAsync(
                request: request,
                cancellationToken: cancellationToken);

            return Result<Guid>.Success(value: productId);
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.BadRequest => Error.Validation(code: "Products.ValidationFailed", description: ex.Message),
                HttpStatusCode.Conflict => Error.Conflict(code: "Products.SkuAlreadyTaken", description: ex.Message),
                _ => Error.Failure(
                    code: "Products.CreateFailed",
                    description: $"The eShop catalog could not create the product: {ex.Message}"),
            };
        }
        catch (JsonException ex)
        {
            return Error.Unexpected(
                code: "Products.InvalidResponse",
                description: $"The eShop catalog returned an unexpected response: {ex.Message}");
        }
    }
}
