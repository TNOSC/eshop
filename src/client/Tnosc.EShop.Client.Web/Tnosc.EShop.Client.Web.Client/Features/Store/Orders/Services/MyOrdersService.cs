// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Ordering;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders.Services;

/// <inheritdoc cref="IMyOrdersService" />
internal sealed class MyOrdersService(IOrderingApi orderingApi) : IMyOrdersService
{
    public Task<ClientResult<PagedResult<OrderSummary>>> GetMyOrdersAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        orderingApi.GetMyOrdersAsync(page: page, pageSize: pageSize, cancellationToken: cancellationToken);
}
