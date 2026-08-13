// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Tnosc.EShop.Server.Application.Ordering.Queries.GetOrderSummary;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.Queries;

/// <summary>
/// Rolls an order's header up with its line statistics using raw SQL.
/// </summary>
/// <remarks>
/// <para>
/// Raw SQL rather than LINQ because this is the shape the design doc reserves it for — joining tables
/// and aggregating a subset of their columns. Doing the same in LINQ would either pull every line into
/// memory to add up, or lean on the provider translating a grouped projection, which is exactly the
/// case where hand-written SQL is both clearer and faster. The order identifier reaches the database
/// as an <see cref="NpgsqlParameter"/>; nothing is interpolated into the SQL text.
/// </para>
/// <para>
/// <strong>The payment join is a T14 addition.</strong> The plan has this joining
/// order × line × payment; the Payment context does not exist yet, so there is no
/// <c>payment.payments</c> to <c>LEFT JOIN</c>. The order × line half below is complete and covered by
/// <c>GetOrderSummaryQueryHandlerTests</c>; adding payment is one more join and a few more columns on
/// <see cref="OrderSummaryReportDto"/>, additive to both.
/// </para>
/// </remarks>
/// <param name="context">The read context.</param>
internal sealed class GetOrderSummaryQueryHandler(EShopReadDbContext context)
    : IQueryHandler<GetOrderSummaryQuery, OrderSummaryReportDto>
{
    private const string SummarySql = """
        SELECT o.id                                          AS "OrderId",
               o.order_number                                AS "OrderNumber",
               o.customer_id                                 AS "CustomerId",
               o.status                                      AS "Status",
               o.placed_on_utc                               AS "PlacedOnUtc",
               o.total_amount                                AS "TotalAmount",
               o.total_currency                              AS "TotalCurrency",
               COALESCE(SUM(l.unit_price_amount * l.quantity), 0) AS "SubtotalAmount",
               COUNT(l.id)                                   AS "LineCount",
               COALESCE(SUM(l.quantity), 0)                  AS "TotalUnits"
        FROM ordering.orders o
        LEFT JOIN ordering.order_lines l ON l.order_id = o.id
        WHERE o.id = @order_id
        GROUP BY o.id, o.order_number, o.customer_id, o.status,
                 o.placed_on_utc, o.total_amount, o.total_currency
        """;

    /// <inheritdoc />
    public async ValueTask<Result<OrderSummaryReportDto>> HandleAsync(
        GetOrderSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        OrderSummaryRow? row = await context.Database
            .SqlQueryRaw<OrderSummaryRow>(
                sql: SummarySql,
                parameters: [new NpgsqlParameter(parameterName: "order_id", parameterType: NpgsqlDbType.Uuid) { Value = query.OrderId }])
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (row is null)
        {
            return OrderErrors.NotFound(orderId: query.OrderId);
        }

        return new OrderSummaryReportDto(
            OrderId: row.OrderId,
            OrderNumber: row.OrderNumber,
            CustomerId: row.CustomerId,
            Status: row.Status,
            PlacedOnUtc: row.PlacedOnUtc,
            TotalAmount: row.TotalAmount,
            TotalCurrency: row.TotalCurrency,
            SubtotalAmount: row.SubtotalAmount,
            DiscountAmount: row.SubtotalAmount - row.TotalAmount,
            LineCount: Convert.ToInt32(value: row.LineCount),
            TotalUnits: Convert.ToInt32(value: row.TotalUnits));
    }
}
