// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Mcp.Application.Products.Ports;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Mcp.Application.Products;

/// <inheritdoc cref="IProductsQueryService" />
internal sealed class ProductsQueryService(IProductsClient productsClient) : IProductsQueryService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyCollection<Product>>> GetProductsAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyCollection<Product> products = await productsClient.GetProductsAsync(
                search: search,
                page: page,
                pageSize: pageSize,
                cancellationToken: cancellationToken);

            return Result<IReadOnlyCollection<Product>>.Success(value: products);
        }
        catch (HttpRequestException ex)
        {
            return Error.Failure(
                code: "Products.Unavailable",
                description: $"The eShop catalog could not be reached: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return Error.Unexpected(
                code: "Products.InvalidResponse",
                description: $"The eShop catalog returned an unexpected response: {ex.Message}");
        }
    }
}
