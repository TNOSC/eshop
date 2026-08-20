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
using Tnosc.EShop.Server.Application.Payment.Commands.InitiatePayment;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Payment.InitiatePayment;

/// <summary>
/// <c>POST /api/payments</c> — initiates a payment for an order.
/// </summary>
/// <remarks>
/// A back-office-shaped write, gated by <see cref="Permissions.Payment.Write"/> rather than resolved
/// from the caller's token — a payment carries no customer identifier of its own to scope against.
/// In the ordinary flow a payment is opened automatically once an order is placed
/// (<c>OrderPlacedInitiatePaymentHandler</c>); this endpoint exists for driving a payment explicitly,
/// including this slice's card-declined walkthrough.
/// </remarks>
internal sealed class InitiatePaymentEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: PaymentRoutes.Payments, handler: HandleAsync)
           .WithName(endpointName: "InitiatePayment")
           .WithTags(PaymentRoutes.Tag)
           .WithSummary(summary: "Initiate a payment for an order")
           .WithDescription(
               description: "Opens a payment for an order under the chosen method. A card is " +
                             "authorized and, if approved, still needs a separate capture; a wallet " +
                             "captures immediately; cash on delivery stays pending until captured at " +
                             "delivery time. A decline is a 201 carrying a Failed payment, not an " +
                             "error — read it back to see the outcome. Fails with 409 when a payment " +
                             "already exists for the order.")
           .Produces<Guid>(statusCode: StatusCodes.Status201Created)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict)
           .HasPermission(permission: Permissions.Payment.Write);

    private static async Task<IResult> HandleAsync(
        InitiatePaymentRequest request,
        ICommandHandler<InitiatePaymentCommand, PaymentId> handler,
        CancellationToken cancellationToken)
    {
        Result<PaymentId> result = await handler.HandleAsync(
            command: request.ToCommand(), cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static paymentId =>
            Results.Created(uri: $"{PaymentRoutes.Payments}/{paymentId.Value}", value: paymentId.Value));
    }
}
