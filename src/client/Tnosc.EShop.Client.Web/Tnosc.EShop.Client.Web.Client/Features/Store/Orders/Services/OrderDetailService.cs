// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Store.Orders.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Ordering;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders.Services;

/// <inheritdoc cref="IOrderDetailService" />
internal sealed class OrderDetailService(IOrderingApi orderingApi) : IOrderDetailService
{
    public async Task<ClientResult<OrderDetailViewModel>> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        ClientResult<Order> result = await orderingApi.GetOrderByIdAsync(id: id, cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ClientResult<OrderDetailViewModel>.Failure(problem: result.Problem!);
        }

        return ClientResult<OrderDetailViewModel>.Success(value: ToViewModel(order: result.Value));
    }

    public Task<ClientResult> ConfirmOrderAsync(Guid id, CancellationToken cancellationToken) =>
        orderingApi.ConfirmOrderAsync(id: id, cancellationToken: cancellationToken);

    public Task<ClientResult> CancelOrderAsync(Guid id, CancellationToken cancellationToken) =>
        orderingApi.CancelOrderAsync(id: id, cancellationToken: cancellationToken);

    private static OrderDetailViewModel ToViewModel(Order order) =>
        new()
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            TotalCurrency = order.TotalCurrency,
            PlacedOnUtc = order.PlacedOnUtc,
            ShippingStreet = order.ShippingStreet,
            ShippingCity = order.ShippingCity,
            ShippingPostalCode = order.ShippingPostalCode,
            ShippingCountry = order.ShippingCountry,
            Lines = [.. order.Lines.Select(ToViewModel)],
        };

    private static OrderLineViewModel ToViewModel(OrderLine line) =>
        new()
        {
            Id = line.Id,
            Sku = line.Sku,
            ProductName = line.ProductName,
            UnitPriceCurrency = line.UnitPriceCurrency,
            Quantity = line.Quantity,
            LineTotalAmount = line.LineTotalAmount,
        };
}
