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
using Tnosc.EShop.Server.Application.Payment.Queries.GetPaymentByOrder;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Payment.GetPaymentByOrder;

/// <summary>
/// <c>GET /api/orders/{orderId}/payment</c> — reads the payment initiated for an order.
/// </summary>
internal sealed class GetPaymentByOrderEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapGet(pattern: PaymentRoutes.PaymentByOrder, handler: HandleAsync)
           .WithName(endpointName: "GetPaymentByOrder")
           .WithTags(PaymentRoutes.Tag)
           .WithSummary(summary: "Read an order's payment")
           .WithDescription(description: "Returns the payment initiated for an order. 404 when none has been.")
           .Produces<PaymentDto>(statusCode: StatusCodes.Status200OK)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .HasPermission(permission: Permissions.Payment.Read);

    private static async Task<IResult> HandleAsync(
        Guid orderId,
        IQueryHandler<GetPaymentByOrderQuery, PaymentDto> handler,
        CancellationToken cancellationToken)
    {
        Result<PaymentDto> result = await handler.HandleAsync(
            query: new GetPaymentByOrderQuery(OrderId: orderId), cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static payment => Results.Ok(value: payment));
    }
}
