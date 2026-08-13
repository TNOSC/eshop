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
using Tnosc.EShop.Server.Application.Identity.Commands.AddCustomerAddress;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.AddCustomerAddress;

/// <summary>
/// <c>POST /api/identity/customers/me/addresses</c> — adds an address to the caller's profile.
/// </summary>
internal sealed class AddCustomerAddressEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: IdentityRoutes.CurrentCustomerAddresses, handler: HandleAsync)
           .WithName(endpointName: "AddCustomerAddress")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Add an address to the caller's profile")
           .WithDescription(
               description: "Adds an address to the caller's own profile. The first address a customer " +
                             "adds automatically becomes their default.")
           .Produces<Guid>(statusCode: StatusCodes.Status201Created)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        AddCustomerAddressRequest request,
        IUserContext userContext,
        ICommandHandler<AddCustomerAddressCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await handler.HandleAsync(
            command: request.ToCommand(userContext: userContext),
            cancellationToken: cancellationToken);

        return result.ToCreated(location: static addressId =>
            $"{IdentityRoutes.CurrentCustomerAddresses}/{addressId}");
    }
}
