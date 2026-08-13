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
using Tnosc.EShop.Server.Application.Identity.Commands.RemoveCustomerAddress;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.RemoveCustomerAddress;

/// <summary>
/// <c>DELETE /api/identity/customers/me/addresses/{addressId}</c> — removes one of the caller's addresses.
/// </summary>
internal sealed class RemoveCustomerAddressEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapDelete(pattern: IdentityRoutes.CurrentCustomerAddressById, handler: HandleAsync)
           .WithName(endpointName: "RemoveCustomerAddress")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Remove one of the caller's addresses")
           .WithDescription(
               description: "Removes an address from the caller's own profile. The default address " +
                             "cannot be removed and returns 409 — make another address the default first.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        Guid addressId,
        IUserContext userContext,
        ICommandHandler<RemoveCustomerAddressCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: new RemoveCustomerAddressCommand(
                ExternalUserId: userContext.UserId,
                AddressId: addressId),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
