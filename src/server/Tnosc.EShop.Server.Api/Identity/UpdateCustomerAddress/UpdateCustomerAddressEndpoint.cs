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
using Tnosc.EShop.Server.Application.Identity.Commands.UpdateCustomerAddress;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Contexts;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.UpdateCustomerAddress;

/// <summary>
/// <c>PUT /api/identity/customers/me/addresses/{addressId}</c> — replaces one of the caller's addresses.
/// </summary>
internal sealed class UpdateCustomerAddressEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPut(pattern: IdentityRoutes.CurrentCustomerAddressById, handler: HandleAsync)
           .WithName(endpointName: "UpdateCustomerAddress")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Update one of the caller's addresses")
           .WithDescription(
               description: "Replaces the contents of one of the caller's own addresses. An address " +
                             "belonging to another customer is simply not found — the address is looked " +
                             "up within the caller's own profile.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        Guid addressId,
        UpdateCustomerAddressRequest request,
        IUserContext userContext,
        ICommandHandler<UpdateCustomerAddressCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: request.ToCommand(userContext: userContext, addressId: addressId),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
