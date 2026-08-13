// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Application.Ordering.Queries.GetOrderById;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.ReadModels;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.Queries;

/// <summary>
/// Projects one of the caller's orders, with its lines, straight from the read context.
/// </summary>
/// <remarks>
/// LINQ rather than raw SQL: this is a parent and its owned children, not the multi-table join the
/// raw-SQL convention is reserved for. The customer identifier is part of the <c>WHERE</c> clause,
/// so an order belonging to someone else is never selected and the answer is indistinguishable from
/// "no such order".
/// </remarks>
/// <param name="context">The read context.</param>
internal sealed class GetOrderByIdQueryHandler(EShopReadDbContext context)
    : IQueryHandler<GetOrderByIdQuery, OrderDto>
{
    /// <inheritdoc />
    public async ValueTask<Result<OrderDto>> HandleAsync(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        OrderDto? order = await context.Set<OrderReadModel>()
            .Where(predicate: readModel => readModel.Id == query.OrderId && readModel.CustomerId == query.CustomerId)
            .Select(selector: readModel => new OrderDto(
                Id: readModel.Id,
                OrderNumber: readModel.OrderNumber,
                CustomerId: readModel.CustomerId,
                Status: readModel.Status,
                TotalAmount: readModel.TotalAmount,
                TotalCurrency: readModel.TotalCurrency,
                PlacedOnUtc: readModel.PlacedOnUtc,
                ShippingStreet: readModel.ShippingStreet,
                ShippingCity: readModel.ShippingCity,
                ShippingPostalCode: readModel.ShippingPostalCode,
                ShippingCountry: readModel.ShippingCountry,
                Lines: readModel.Lines
                    .Select(line => new OrderLineDto(
                        Id: line.Id,
                        ProductId: line.ProductId,
                        Sku: line.Sku,
                        ProductName: line.ProductName,
                        UnitPriceAmount: line.UnitPriceAmount,
                        UnitPriceCurrency: line.UnitPriceCurrency,
                        Quantity: line.Quantity,
                        LineTotalAmount: line.UnitPriceAmount * line.Quantity))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (order is null)
        {
            return OrderErrors.NotFound(orderId: query.OrderId);
        }

        return order;
    }
}
