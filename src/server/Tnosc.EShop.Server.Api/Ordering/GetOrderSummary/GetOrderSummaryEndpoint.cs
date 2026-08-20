// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tnosc.EShop.Server.Application.Ordering.Queries.GetOrderSummary;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Ordering.GetOrderSummary;

/// <summary>
/// <c>GET /api/orders/{id}/summary</c> — the rolled-up back-office view of any order.
/// </summary>
/// <remarks>
/// Permission-gated rather than scoped to the caller, because it deliberately reads <em>any</em>
/// customer's order — that is what makes it a back-office view rather than a second way to read your
/// own.
/// </remarks>
internal sealed class GetOrderSummaryEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapGet(pattern: OrderingRoutes.OrderSummary, handler: HandleAsync)
           .WithName(endpointName: "GetOrderSummary")
           .WithTags(OrderingRoutes.Tag)
           .WithSummary(summary: "Read an order's rolled-up summary")
           .WithDescription(
               description: "Returns any order's header rolled up with its line statistics — subtotal, " +
                             "discount, line count and total units — in a single query.")
           .Produces<OrderSummaryReportDto>(statusCode: StatusCodes.Status200OK)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .HasPermission(permission: Permissions.Ordering.Read);

    private static async Task<IResult> HandleAsync(
        Guid id,
        IQueryHandler<GetOrderSummaryQuery, OrderSummaryReportDto> handler,
        CancellationToken cancellationToken)
    {
        Result<OrderSummaryReportDto> result = await handler.HandleAsync(
            query: new GetOrderSummaryQuery(OrderId: id),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static summary => Results.Ok(value: summary));
    }
}
