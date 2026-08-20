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
using Tnosc.EShop.Server.Application.Identity.Commands.SetDefaultCustomerAddress;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.SetDefaultCustomerAddress;

/// <summary>
/// <c>PUT /api/identity/customers/me/addresses/{addressId}/default</c> — makes one of the caller's
/// addresses their default.
/// </summary>
internal sealed class SetDefaultCustomerAddressEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPut(pattern: IdentityRoutes.CurrentCustomerDefaultAddress, handler: HandleAsync)
           .WithName(endpointName: "SetDefaultCustomerAddress")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Make one of the caller's addresses their default")
           .WithDescription(
               description: "Selects which of the caller's addresses is their default. Use this before " +
                             "removing the current default, which cannot be deleted while it holds that role.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        Guid addressId,
        IUserContext userContext,
        ICommandHandler<SetDefaultCustomerAddressCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: new SetDefaultCustomerAddressCommand(
                ExternalUserId: userContext.UserId,
                AddressId: addressId),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
