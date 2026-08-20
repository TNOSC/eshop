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
using Tnosc.EShop.Server.Application.Payment.Commands.RefundPayment;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Payment.RefundPayment;

/// <summary>
/// <c>POST /api/payments/{id}/refund</c> — refunds a captured payment.
/// </summary>
internal sealed class RefundPaymentEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: PaymentRoutes.PaymentRefund, handler: HandleAsync)
           .WithName(endpointName: "RefundPayment")
           .WithTags(PaymentRoutes.Tag)
           .WithSummary(summary: "Refund a captured payment")
           .WithDescription(
               description: "Returns a previously captured payment's funds to the customer. Returns " +
                             "409 when the payment was never captured, or has already been refunded.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict)
           .HasPermission(permission: Permissions.Payment.Write);

    private static async Task<IResult> HandleAsync(
        Guid id,
        RefundPaymentRequest request,
        ICommandHandler<RefundPaymentCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: request.ToCommand(paymentId: id), cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
