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
using Tnosc.EShop.Server.Application.Payment.Commands.CapturePayment;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Payment.CapturePayment;

/// <summary>
/// <c>POST /api/payments/{id}/capture</c> — captures a payment.
/// </summary>
/// <remarks>
/// Completes a card's authorize-then-capture flow, or settles a cash-on-delivery payment at delivery
/// time. Capturing an already-captured, failed or refunded payment returns 409.
/// </remarks>
internal sealed class CapturePaymentEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: PaymentRoutes.PaymentCapture, handler: HandleAsync)
           .WithName(endpointName: "CapturePayment")
           .WithTags(PaymentRoutes.Tag)
           .WithSummary(summary: "Capture a payment")
           .WithDescription(
               description: "Captures a previously authorized card payment, or settles a cash-on-" +
                             "delivery payment at delivery time. A gateway decline is recorded as a " +
                             "Failed payment, not an error. Returns 409 when the payment cannot be " +
                             "captured from its current status.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict)
           .HasPermission(permission: Permissions.Payment.Write);

    private static async Task<IResult> HandleAsync(
        Guid id,
        ICommandHandler<CapturePaymentCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: new CapturePaymentCommand(PaymentId: id),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
