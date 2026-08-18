// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Store.Orders.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Ordering;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders.Services;

/// <inheritdoc cref="IMyOrdersService" />
internal sealed class MyOrdersService(IOrderingApi orderingApi) : IMyOrdersService
{
    public async Task<ClientResult<PagedResult<OrderSummaryViewModel>>> GetMyOrdersAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        ClientResult<PagedResult<OrderSummary>> result = await orderingApi.GetMyOrdersAsync(
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ClientResult<PagedResult<OrderSummaryViewModel>>.Failure(problem: result.Problem!);
        }

        PagedResult<OrderSummaryViewModel> mapped = new(
            Items: [.. result.Value.Items.Select(ToViewModel)],
            Page: result.Value.Page,
            PageSize: result.Value.PageSize,
            TotalCount: result.Value.TotalCount,
            TotalPages: result.Value.TotalPages);

        return ClientResult<PagedResult<OrderSummaryViewModel>>.Success(value: mapped);
    }

    private static OrderSummaryViewModel ToViewModel(OrderSummary order) =>
        new()
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            TotalCurrency = order.TotalCurrency,
            PlacedOnUtc = order.PlacedOnUtc,
        };
}
