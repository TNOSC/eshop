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
using Tnosc.EShop.Server.Application.Identity.Commands.DeactivateCustomer;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.DeactivateCustomer;

/// <summary>
/// <c>POST /api/identity/customers/{id}/deactivate</c> — deactivates a customer's profile.
/// </summary>
/// <remarks>
/// Local only: this does not disable the Keycloak account, which an operator does separately in the
/// admin console. Requires the <c>identity:write</c> permission.
/// </remarks>
internal sealed class DeactivateCustomerEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: IdentityRoutes.CustomerDeactivateById, handler: HandleAsync)
           .WithName(endpointName: "DeactivateCustomer")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Deactivate a customer's profile")
           .WithDescription(
               description: "Deactivates a customer's profile, closing it to further change. Does NOT " +
                             "disable the customer's Keycloak account — an operator does that separately " +
                             "in the admin console. Requires the 'identity:write' permission, which the " +
                             "'admin' realm role grants.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict)
           .HasPermission(permission: Permissions.Identity.Write);

    private static async Task<IResult> HandleAsync(
        Guid id,
        ICommandHandler<DeactivateCustomerCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: new DeactivateCustomerCommand(CustomerId: id),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
