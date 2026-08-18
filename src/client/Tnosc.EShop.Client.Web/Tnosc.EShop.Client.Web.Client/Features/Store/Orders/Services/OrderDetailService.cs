// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Ordering;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders.Services;

/// <inheritdoc cref="IOrderDetailService" />
internal sealed class OrderDetailService(IOrderingApi orderingApi) : IOrderDetailService
{
    public Task<ClientResult<Order>> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken) =>
        orderingApi.GetOrderByIdAsync(id: id, cancellationToken: cancellationToken);

    public Task<ClientResult> ConfirmOrderAsync(Guid id, CancellationToken cancellationToken) =>
        orderingApi.ConfirmOrderAsync(id: id, cancellationToken: cancellationToken);

    public Task<ClientResult> CancelOrderAsync(Guid id, CancellationToken cancellationToken) =>
        orderingApi.CancelOrderAsync(id: id, cancellationToken: cancellationToken);
}
