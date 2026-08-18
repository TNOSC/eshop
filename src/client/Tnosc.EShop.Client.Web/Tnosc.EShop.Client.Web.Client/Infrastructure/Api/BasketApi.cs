// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Contracts.Basket;
using Tnosc.EShop.Client.Web.Contracts.Routes;
using Tnosc.Lib.Web.Api;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>
/// <see cref="IBasketApi"/> against the BFF's Basket endpoints. Contains no absolute URI and never
/// contains the string <c>bff</c> — the difference between the two hosts is entirely in the injected
/// <see cref="HttpClient.BaseAddress"/>.
/// </summary>
internal sealed class BasketApi(HttpClient httpClient) : IBasketApi
{
    public async Task<ClientResult<BasketDto>> GetBasketAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            requestUri: ApiRoutes.Basket.Current,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<BasketDto>(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult<BasketDto>> AddItemAsync(
        AddItemToBasketRequest request,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            requestUri: ApiRoutes.Basket.Items,
            value: request,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<BasketDto>(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult<BasketDto>> ChangeItemQuantityAsync(
        Guid itemId,
        ChangeBasketItemQuantityRequest request,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PutAsJsonAsync(
            requestUri: ApiRoutes.Basket.ItemById(itemId: itemId),
            value: request,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<BasketDto>(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult> RemoveItemAsync(Guid itemId, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.DeleteAsync(
            requestUri: ApiRoutes.Basket.ItemById(itemId: itemId),
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult> ClearAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.DeleteAsync(
            requestUri: ApiRoutes.Basket.Current,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }
}
