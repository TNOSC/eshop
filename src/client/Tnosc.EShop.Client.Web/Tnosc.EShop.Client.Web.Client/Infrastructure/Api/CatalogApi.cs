// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.EShop.Client.Web.Contracts.Routes;
using Tnosc.Lib.Web.Api;
using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>
/// <see cref="ICatalogApi"/> against the BFF's Catalog endpoints. Contains no absolute URI and never
/// contains the string <c>bff</c> — the difference between the two hosts is entirely in the injected
/// <see cref="HttpClient.BaseAddress"/>.
/// </summary>
internal sealed class CatalogApi(HttpClient httpClient) : ICatalogApi
{
    public async Task<ApiResult<PagedResult<ProductSummary>>> SearchProductsAsync(
        SearchProductsQuery query,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            requestUri: ApiRoutes.Catalog.SearchProducts(query: query),
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<PagedResult<ProductSummary>>(
            response: response,
            cancellationToken: cancellationToken);
    }

    public async Task<ApiResult<Product>> GetProductAsync(Guid id, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            requestUri: ApiRoutes.Catalog.ProductById(id: id),
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<Product>(
            response: response,
            cancellationToken: cancellationToken);
    }

    public async Task<ApiResult<IReadOnlyList<Category>>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            requestUri: ApiRoutes.Catalog.Categories,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<IReadOnlyList<Category>>(
            response: response,
            cancellationToken: cancellationToken);
    }

    public async Task<ApiResult<Guid>> CreateProductAsync(
        CreateProductRequest request,
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage message = new(method: HttpMethod.Post, requestUri: ApiRoutes.Catalog.Products)
        {
            Content = JsonContent.Create(inputValue: request),
        };
        message.Headers.Add(name: IdempotencyHeader.Name, value: idempotencyKey.ToString());

        using HttpResponseMessage response = await httpClient.SendAsync(
            request: message,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<Guid>(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ApiResult> UpdateProductPriceAsync(
        Guid productId,
        UpdateProductPriceRequest request,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PutAsJsonAsync(
            requestUri: ApiRoutes.Catalog.ProductPrice(id: productId),
            value: request,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ApiResult> AdjustStockAsync(
        Guid productId,
        AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            requestUri: ApiRoutes.Catalog.ProductStock(id: productId),
            value: request,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }
}
