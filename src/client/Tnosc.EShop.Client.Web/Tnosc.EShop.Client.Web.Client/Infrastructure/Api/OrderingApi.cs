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
using Tnosc.EShop.Client.Web.Contracts.Ordering;
using Tnosc.EShop.Client.Web.Contracts.Routes;
using Tnosc.Lib.Web.Api;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>
/// <see cref="IOrderingApi"/> against the BFF's Ordering endpoints. Contains no absolute URI and
/// never contains the string <c>bff</c> — the difference between the two hosts is entirely in the
/// injected <see cref="HttpClient.BaseAddress"/>.
/// </summary>
internal sealed class OrderingApi(HttpClient httpClient) : IOrderingApi
{
    public async Task<ClientResult<PagedResult<OrderSummary>>> GetMyOrdersAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            requestUri: ApiRoutes.Ordering.MyOrders(page: page, pageSize: pageSize),
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<PagedResult<OrderSummary>>(
            response: response,
            cancellationToken: cancellationToken);
    }

    public async Task<ClientResult<Order>> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            requestUri: ApiRoutes.Ordering.OrderById(id: id),
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<Order>(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult<Guid>> PlaceOrderAsync(Guid idempotencyKey, CancellationToken cancellationToken)
    {
        using HttpRequestMessage message = new(method: HttpMethod.Post, requestUri: ApiRoutes.Ordering.Orders);
        message.Headers.Add(name: IdempotencyHeader.Name, value: idempotencyKey.ToString());

        using HttpResponseMessage response = await httpClient.SendAsync(
            request: message,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<Guid>(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult> ConfirmOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsync(
            requestUri: ApiRoutes.Ordering.OrderConfirm(id: id),
            content: null,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult> CancelOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsync(
            requestUri: ApiRoutes.Ordering.OrderCancel(id: id),
            content: null,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }
}
